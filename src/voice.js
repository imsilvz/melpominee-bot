import { spawn, exec } from 'child_process';
import { createReadStream, existsSync, readdirSync } from 'node:fs';
import { dirname, join, parse } from 'node:path';
import { fileURLToPath } from 'node:url';
import { 
    demuxProbe, createAudioResource, createAudioPlayer,
    joinVoiceChannel, getVoiceConnection, VoiceConnectionStatus, 
    NoSubscriberBehavior, AudioPlayerStatus
} from '@discordjs/voice';
import { UpdateMusicEmbed } from "./util.js";

import { db } from './db.js';

export const JoinDiscordVoiceChannel = async (channel) => {
    const conn = joinVoiceChannel({
        channelId: channel.id,
        guildId: channel.guild.id,
        adapterCreator: channel.guild.voiceAdapterCreator
    });

    conn.on(VoiceConnectionStatus.Ready, async () => {
        console.log(`[${channel.guild.id}] Connection ready`);
        await UpdateMusicEmbed(channel.guild);
    });

    conn.on(VoiceConnectionStatus.Disconnected, async (oldState, newState) => {
        try {
            await Promise.race([
                entersState(conn, VoiceConnectionStatus.Signalling, 5_000),
                entersState(conn, VoiceConnectionStatus.Connecting, 5_000),
            ]);
            // Seems to be reconnecting to a new channel - ignore disconnect
        } catch (error) {
            // Seems to be a real disconnect which SHOULDN'T be recovered from
            conn.destroy();
            console.log(`[${channel.guild.id}] Connection destroyed`);
            await UpdateMusicEmbed(channel.guild);
        }
    });
}

async function helper_GetMusicMetadata(filepath) {
    const child = spawn('ffprobe', [
        "-v", "quiet",
        "-print_format", "json", 
        "-show_format",
        `"${filepath}"`
    ], { 
        stdio: 'pipe',
        shell: false,
        //windowsVerbatimArguments: true
    });

    let data = "";
    for await (const chunk of child.stdout) {
        data += chunk;
    }

    let error = "";
    for await (const chunk of child.stderr) {
        error += chunk;
    }

    const exitCode = await new Promise((resolve, reject) => {
        child.on('close', resolve);
    });

    if(exitCode) {
        throw new Error(`subprocess error exit ${exitCode}, ${error}`);
    }

    return JSON.parse(data);
}

export const MusicQueue = new Map();
async function helper_CreateAudioResource(readableStream) {
	const { stream, type } = await demuxProbe(readableStream);
	return createAudioResource(stream, { inputType: type, inlineVolume: true });
}

function helper_ShuffleArray(array) {
    for (let i = array.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [array[i], array[j]] = [array[j], array[i]];
    }
}

const CreateQueueItem = async (dir, fileName, volumeMap) => {
    const audioJson = await helper_GetMusicMetadata(join(dir, fileName));
    const readStream = createReadStream(join(dir, fileName));
    const audioResource = await helper_CreateAudioResource(readStream);
    let songName = parse(fileName).name;
    let songVolume = volumeMap.get(songName);
    if(volumeMap.has(songName)) {
        audioResource.volume.setVolume(songVolume);
    } else {
        if(volumeMap.has('')) {
            let defaultVolume = volumeMap.get('');
            audioResource.volume.setVolume(defaultVolume);
        } else {
            audioResource.volume.setVolume(0.05);
        }
    }
    return {
        name: songName,
        resource: audioResource,
        duration: audioJson.format.duration
    };
}

export const StartPlaylist = async (guild, playlist, loop=true) => {
    const conn = getVoiceConnection(guild.id);
    const dir = join(dirname(fileURLToPath(import.meta.url)), "../", "assets", playlist, "audio");
    if(!existsSync(dir)) {
        return false;
    }

    let volumeData = await db.all(`
        SELECT song, volume
        FROM volumes
        WHERE guild = ?
            AND playlist = ?
    `, guild.id, playlist);
    let volumeMap = volumeData.reduce((prev, val) => {
        prev.set(val.song, val.volume);
        return prev;
    }, new Map());
    
    const queue = [];
    const files = readdirSync(dir, { withFileTypes: true })
    for(let i=0; i<files.length; i++) {
        let file = files[i];
        if(file.isFile()) {
            queue.push(CreateQueueItem(dir, file.name, volumeMap));
        }
    }

    // randomize and await!
    helper_ShuffleArray(queue);

    var player = null;
    let queueInfo = MusicQueue.get(guild.id);
    if(queueInfo) {
        player = queueInfo.player;
        queueInfo.playlist = playlist;
        queueInfo.queueIdx = 0;
        queueInfo.queue = await Promise.all(queue);
    } else {
        player = createAudioPlayer({
            behaviors: {
                noSubscriber: NoSubscriberBehavior.Play
            }
        });
        
        player.on(AudioPlayerStatus.Idle, async () => {
            let currentQueue = MusicQueue.get(guild.id);
            if(currentQueue) {
                currentQueue.queueIdx++;
                if(currentQueue.queue.length > currentQueue.queueIdx) {
                    player.play(currentQueue.queue[currentQueue.queueIdx].resource);
                } else {
                    let playlist = currentQueue.playlist;
                    MusicQueue.delete(guild.id);
                    player.stop();
                    if(currentQueue.loop) {
                        await StartPlaylist(guild, playlist, loop);
                    }
                }
            }
            await UpdateMusicEmbed(guild);
        });

        player.on(AudioPlayerStatus.Playing, async () => {
            let queue = MusicQueue.get(guild.id);
            queue.startTime = new Date();
            await UpdateMusicEmbed(guild);
        });

        MusicQueue.set(guild.id, {
            "player": player,
            "playlist": playlist,
            "queueIdx": 0,
            "queue": await Promise.all(queue),
            "loop": loop
        });
    }
    conn.subscribe(player);

    player.play(MusicQueue.get(guild.id).queue[0].resource);
    return true;
} 

export const SkipSong = async (guild) => {
    let queueInfo = MusicQueue.get(guild.id);
    if(queueInfo) {
        queueInfo.player.stop();
        return true;
    }
    return false;
}

export const StopPlaylist = async (guild) => {
    let queueInfo = MusicQueue.get(guild.id);
    if(queueInfo) {
        MusicQueue.delete(guild.id);
        queueInfo.player.stop();
        await UpdateMusicEmbed(guild);
        return true;
    }
    return false;
}

export const LeaveDiscordVoiceChannel = async (channel) => {
    const conn = getVoiceConnection(channel.guild.id);
    if(conn) {
        conn.disconnect();
    } else {
        let newConn = joinVoiceChannel({
            channelId: channel.id,
            guildId: channel.guild.id,
            adapterCreator: channel.guild.voiceAdapterCreator
        });
    
        newConn.on(VoiceConnectionStatus.Ready, async () => {
            let queueInfo = MusicQueue.get(guild.id);
            if(queueInfo) {
                MusicQueue.delete(guild.id);
            }
            newConn.disconnect();
            newConn.destroy();
            await UpdateMusicEmbed(channel.guild);
        });
    }
}
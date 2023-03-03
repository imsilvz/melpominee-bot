import { spawn } from 'child_process';
import { getVoiceConnection } from '@discordjs/voice';
import { RollDicePool } from "./dice.js";
import { DeleteMusicEmbed, SendV5Reply, UpdateMusicEmbed } from "./util.js";
import { JoinDiscordVoiceChannel, LeaveDiscordVoiceChannel, MusicQueue, SkipSong, StartPlaylist, StopPlaylist } from "./voice.js";

import { db } from './db.js';

const commands = {
    "config": async (interaction) => {
        let mode = interaction.options.getSubcommand();
        let setting = interaction.options.getString('setting', true);
        let args = interaction.options.getString('args');
        let guild = await interaction.guild.fetch();

        switch(mode) {
            case "set":
                if(setting == "music_channel") {
                    const channels = guild.channels;
                    const snowflakeRegex = /<#(.+)>/;

                    let regexResult = snowflakeRegex.exec(args);
                    if(regexResult) {
                        let snowflakeId = regexResult[1];
                        try {
                            let channel = await channels.fetch(snowflakeId);
                            await DeleteMusicEmbed(guild);
                            await db.run(`
                                INSERT OR REPLACE INTO settings
                                    (guild, setting, value)
                                VALUES
                                    (?, ?, ?)
                            `, guild.id, setting, snowflakeId);
                            interaction.reply({
                                content: `Setting '${setting}' updated to: ${channel}`,
                                ephemeral: true
                            });
                            await UpdateMusicEmbed(guild);
                        } catch(e) {
                            if(e.code != 10003) {
                                throw e;
                            }
                            return interaction.reply({
                                content: "Invalid channel specified.",
                                ephemeral: true
                            });
                        }


                    } else {
                        interaction.reply({
                            content: "Invalid channel specified.",
                            ephemeral: true
                        });
                    }
                } else {
                    interaction.reply({
                        content: "Invalid Setting!",
                        ephemeral: true
                    });
                }
                break;
            case "get":
                let dbResult = await db.get(`
                    SELECT value
                    FROM settings
                    WHERE guild = ? AND setting = ?
                `, guild.id, setting);
                if(dbResult) {
                    return interaction.reply({
                        content: `Current '${setting}' value: ${dbResult.value}`,
                        ephemeral: true
                    });
                }
            default:
                interaction.reply({
                    content: "Invalid setting or value not found!",
                    ephemeral: true
                });
        }
    },
    "v5": async (interaction) => {
        // options
        let pool = interaction.options.getNumber('pool', true);
        let hunger = interaction.options.getNumber('hunger', false);
        let difficulty = interaction.options.getNumber('difficulty', false);

        // get roll results
        const { successes, dice, hungerDice, bestial, messy, critical } = await RollDicePool(pool, hunger);
        await SendV5Reply(interaction, pool, hunger, difficulty, successes, dice, hungerDice, bestial, messy, critical, false);
    },
    "summon": async (interaction) => {
        if(!await interaction.inGuild()) {
            return interaction.reply({
                content: "This command must be used in a discord server.",
                ephemeral: true
            });
        }

        const user = await interaction.member.fetch();
        const voiceChannelId = user.voice.channelId;
        if(voiceChannelId) {
            await JoinDiscordVoiceChannel(await interaction.guild.channels.fetch(voiceChannelId));
            interaction.reply({
                content: "Joining voice channel!",
                ephemeral: true
            });
        } else {
            interaction.reply({
                content: "This command may only be used while connected to a voice channel.",
                ephemeral: true
            });
        }
    },
    "dismiss": async (interaction) => {
        if(!await interaction.inGuild()) {
            return interaction.reply({
                content: "This command must be used in a discord server.",
                ephemeral: true
            });
        }

        const user = await interaction.guild.members.me;
        const voiceChannelId = user.voice.channelId;
        if(voiceChannelId) {
            await LeaveDiscordVoiceChannel(await interaction.guild.channels.fetch(voiceChannelId));
            interaction.reply({
                content: "Leaving voice channel!",
                ephemeral: true
            });
        } else {
            interaction.reply({
                content: "I am not currently connected to a voice channel in this server.",
                ephemeral: true
            });
        }
    },
    "download": async (interaction) => {
        let url = interaction.options.getString('video', true);
        let playlist = interaction.options.getString('playlist', true);
        let split = interaction.options.getBoolean('chapters', false);
        let thumbnail = interaction.options.getBoolean('thumbnail', false);
        await interaction.reply({
            content: "Beginning download...",
            ephemeral: true
        });

        async function downloadYoutube() {
            const args = [
                "--extract-audio", 
                "--audio-format", "m4a", 
                "--audio-quality", "0"
            ]
            if(thumbnail) {
                args.push(
                    "--write-thumbnail", "--convert-thumbnails", "png",
                    "-o", `thumbnail:./assets/${playlist}/thumb.%(ext)s`
                );
            }
            if(split) { 
                args.push(
                    "--split-chapters", 
                    "-o", `chapter:./assets/${playlist}/audio/%(section_title)s.%(ext)s`
                );
                args.push("-o", `./assets/${playlist}/audio/original/%(title)s.%(ext)s`);
            } else {
                args.push("-o", `./assets/${playlist}/audio/%(title)s.%(ext)s`);
            }
            args.push(url);
            const child = spawn('yt-dlp', args, { cwd: process.cwd(), shell: false });
        
            let data = "";
            for await (const chunk of child.stdout) {
                data += chunk;
            }

            let error = "";
            for await (const chunk of child.stderr) {
                error += chunk;
            }

            const exitCode = await new Promise( (resolve, reject) => {
                child.on('close', resolve);
            });
        
            if(exitCode) {
                throw new Error( `subprocess error exit ${exitCode}, ${error}`);
            }
            return data;
        }
        console.log(await downloadYoutube());

        await interaction.editReply({
            content: "Download Complete",
            ephemeral: true
        });
    },
    "play": async (interaction) => {
        let playlist = interaction.options.getString('playlist', true);
        const conn = getVoiceConnection(interaction.guild.id);
        if(!conn) {
            return interaction.reply({
                content: "I am not currently connected to a voice channel in this server.",
                ephemeral: true
            });
        }
        await StartPlaylist(interaction.guild, playlist);
        interaction.reply({
            content: "Okay!",
            ephemeral: true
        });
    },
    "skip": async (interaction) => {
        const conn = getVoiceConnection(interaction.guild.id);
        if(!conn) {
            return interaction.reply({
                content: "I am not currently connected to a voice channel in this server.",
                ephemeral: true
            });
        }
        if(await SkipSong(interaction.guild)) {
            return interaction.reply({
                content: "Skipping current song!",
                ephemeral: true
            });
        }
        interaction.reply({
            content: "No playlist is currently playing.",
            ephemeral: true
        });
    },
    "stop": async (interaction) => {
        const conn = getVoiceConnection(interaction.guild.id);
        if(!conn) {
            return interaction.reply({
                content: "I am not currently connected to a voice channel in this server.",
                ephemeral: true
            });
        }
        if(await StopPlaylist(interaction.guild)) {
            return interaction.reply({
                content: "Playlist stopped.",
                ephemeral: true
            });
        }
        return interaction.reply({
            content: "No music is currently playing to stop.",
            ephemeral: true
        });
    },
    "volume": async (interaction) => {
        let volume = interaction.options.getNumber('volume', true);
        let songVolume = interaction.options.getBoolean('song', false);
        let queue = MusicQueue.get(interaction.guild.id);
        if(!queue) {
            return interaction.reply({
                content: "No playlist is currently playing!",
                ephemeral: true
            });
        }
        
        let volumeData = await db.all(`
            SELECT song, volume
            FROM volumes
            WHERE guild = ?
                AND playlist = ?
        `, interaction.guild.id, queue.playlist);
        let volumeMap = volumeData.reduce((prev, val) => {
            prev.set(val.song, val.volume);
            return prev;
        }, new Map());

        for(let i=0; i<queue.queue.length; i++) {
            let resource = queue.queue[i].resource;
            if(songVolume && i === queue.queueIdx) {
                if(!volume) {
                    if(volumeMap.has('')) {
                        resource.volume.setVolume(volumeMap.get(''));
                    } else {
                        resource.volume.setVolume(0.05);
                    }
                } else {
                    resource.volume.setVolume(volume);
                }
            } else {
                let songName = queue.queue[i].name;
                if(!songVolume && !volumeMap.has(songName)) {
                    if(!volume) {
                        resource.volume.setVolume(0.05);
                    } else {
                        resource.volume.setVolume(volume);
                    }
                }
            }
        }
        
        let song = '';
        if(songVolume) {
            song = queue.queue[queue.queueIdx].name;
        }

        if(!volume) {
            console.log(await db.run(`
                DELETE FROM volumes
                WHERE guild = ? AND playlist = ? AND song = ?
            `, interaction.guild.id, queue.playlist, song));

            interaction.reply({
                content: `Volume for playlist '${queue.playlist}' reset!`,
                ephemeral: true
            });
        } else {
            await db.run(`
            INSERT OR REPLACE INTO volumes
                (guild, playlist, song, volume)
            VALUES
                (?, ?, ?, ?)
            `, interaction.guild.id, queue.playlist, song, volume);
    
            interaction.reply({
                content: `Volume for playlist '${queue.playlist}' set to: ${volume}`,
                ephemeral: true
            });
        }
    }
}

export const handleInputCommand = async (interaction) => {
    if(commands.hasOwnProperty(interaction.commandName)) {
        console.log(`[${interaction.guild.id}] Recieved command '${interaction.commandName}' from user '${interaction.user.tag}'`);
        return await commands[interaction.commandName](interaction);
    }
    interaction.reply({ content: "Unknown Command", ephemeral: true });
}
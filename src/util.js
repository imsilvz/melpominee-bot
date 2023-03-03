import { createReadStream, existsSync, readdirSync } from 'node:fs';
import { dirname, join, parse } from 'node:path';
import { fileURLToPath } from 'node:url';
import { EmbedBuilder } from "@discordjs/builders";
import { ActionRowBuilder, AttachmentBuilder, ButtonBuilder, ButtonStyle } from 'discord.js';
import { getVoiceConnection, VoiceConnectionStatus } from '@discordjs/voice';
import client from './client.js';
import { MusicQueue } from './voice.js';
import { db } from './db.js';

export const SendV5Reply = async (
        interaction, pool, hunger, difficulty, 
        successes, dice, hungerDice, bestial, 
        messy, critical, reroll, rerollType
) => {
    const user = interaction.user;
    const avatar = await user.avatarURL();

    // get username
    var username = user.username;
    if(interaction.inGuild()) {
        const member = interaction.member;
        username = member.displayName;
    }

    // build embed variables
    var color = 0x202020; // dark gray
    var title = `${username} rolled | Pool ${pool}`;
    var result = `Total: ${successes == 1 ? '**1** success' : `**${successes}** successes`}`;
    if(hunger) { title = title + ` | Hunger ${hunger}`; }
    if(difficulty) { 
        let margin = successes - difficulty;
        title = title + ` | Difficulty ${difficulty}`; 
        result = result + ` vs diff ${difficulty}`;
        if(margin >= 0) {
            if(margin > 0) {
                result += `\nMargin of ${margin} successes`;
            }
            if(critical) {
                if(messy) {
                    color = 0xff0000; // bright red
                    result += '\n**[ Messy Critical! ]**';
                } else {
                    color = 0x600000; // dark red
                    result += '\n**[ Critical Success! ]**';
                }
            } else {
                result += '\n**[ Success! ]**';
            }
        } else if(margin < 0) {
            result += `\nMargin of ${Math.abs(margin)} missing successes`;
            if(bestial) {
                color = 0xff0000; // bright red
                result += '\n**[ Bestial Failure! ]**';
            } else {
                if(successes > 0) {
                    result += '\n**[ Failure! ]**';
                } else {
                    result += '\n**[ Total Failure! ]**';
                }
            }
        }
    } else {
        if(successes > 0) {
            if(critical) {
                if(messy) {
                    color = 0xff0000; // bright red
                    result += '\n**[ Messy Critical! ]**';
                } else {
                    color = 0x600000; // dark red
                    result += '\n**[ Critical Success! ]**';
                }
            } else {
                result += '\n**[ Success! ]**';
            }
        } else {
            result += '\n**[ Total Failure! ]**';
        }
    }

    // build embed fields
    var successDice = dice.reduce((opts, val) => {
        if(val <= 5) {opts.push(val)} 
        return opts;
    }, []);
    var failDice = dice.reduce((opts, val) => {
        if(val >= 6) {opts.push(val)}
        return opts;
    }, []);

    var fields = [
        { name: 'Result', value: result, inline: true },
        { name: 'Dice', value: `\`\`\`${successDice.join(' ')} | ${failDice.join(' ')}\`\`\``, inline: true }
    ]

    var successHunger = [];
    var failHunger = [];
    if(hunger) {
        successHunger = hungerDice.reduce((opts, val) => {
            if(val <= 5) {opts.push(val)}
            return opts;
        }, []);
        failHunger = hungerDice.reduce((opts, val) => {
            if(val >= 6) {opts.push(val)}
            return opts;
        }, []);

        // build hunger items
        let hungerItems = ['-'];
        if(successHunger && successHunger.length) { hungerItems.push(successHunger.join(' ')); }
        hungerItems.push('|');
        if(failHunger && failHunger.length) { hungerItems.push(failHunger.join(' ')); }
        hungerItems.push('-');

        fields.push(
            { name: 'Hunger', value: `\`\`\`diff\n${hungerItems.join(' ')}\`\`\``, inline: true }
        );
    }

    // fetch all emojis
    let bestialEmoji = client.emojis.cache.get('1047224345375821987');
    let failEmoji = client.emojis.cache.get('1047224347707838605');
    let successEmoji = client.emojis.cache.get('1047224352686489710');
    let critEmoji = client.emojis.cache.get('1047224346650890260');
    let hungerFailEmoji = client.emojis.cache.get('1047224350501249034');
    let hungerSuccessEmoji = client.emojis.cache.get('1047224351470133310');
    let hungerCritEmoji = client.emojis.cache.get('1047224348638978048');

    // build emoji string
    let failMessage = '';
    let successMessage = '';
    let successCritMessage = '';
    for(let i=0; i<dice.length; i++) {
        if(dice[i] >= 6) {
            if(dice[i] == 10) {
                successCritMessage = `${successCritMessage}${critEmoji}`;
            } else {
                successMessage = `${successMessage}${successEmoji}`;
            }
        } else {
            failMessage = `${failMessage}${failEmoji}`;
        }
    }

    let hungerBestialMessage = '';
    let hungerFailMessage = '';
    let hungerSuccessMessage = '';
    let hungerCritMessage = '';
    for(let i=0; i<hungerDice.length; i++) {
        if(hungerDice[i] >= 6) {
            if(hungerDice[i] == 10) {
                hungerCritMessage = `${hungerCritMessage}${hungerCritEmoji}`;
            } else {
                hungerSuccessMessage = `${hungerSuccessMessage}${hungerSuccessEmoji}`;
            }
        } else {
            if(hungerDice[i] == 1) {
                hungerBestialMessage = `${hungerBestialMessage}${bestialEmoji}`;
            } else {
                hungerFailMessage = `${hungerFailMessage}${hungerFailEmoji}`;
            }
        }
    }

    // create Embed object
    const embed = new EmbedBuilder()
        .setColor(color)
        .setAuthor({ name: title, iconURL: avatar })
        .addFields(fields);

    if(reroll) {
        embed.setDescription(`**<† Willpower †>** | ${rerollType}`)
    }

    // create Row One Components
    let components = []
    if(dice.find((num) => num <= 5)) {
        components.push(
            new ButtonBuilder()
                .setCustomId('reroll-failure')
                .setLabel('Re-roll Failures')
                .setStyle(ButtonStyle.Secondary)
                .setEmoji('1047224347707838605'),
        )
    }
    if(dice.find((num) => num < 10)) {
        components.push(
            new ButtonBuilder()
                .setCustomId('maximize-crits')
                .setLabel('Maximize Crits')
                .setStyle(ButtonStyle.Secondary)
                .setEmoji('1047224346650890260')
        );
    }

    // create Action Rows
    const rowOne = new ActionRowBuilder()
        .addComponents(...components);

    const rowTwo = new ActionRowBuilder()
        .addComponents(
            new ButtonBuilder()
                .setCustomId('avoid-messy')
                .setLabel('Avoid Messy Critical')
                .setStyle(ButtonStyle.Secondary)
                .setEmoji('1047224348638978048')
        );

    let buttonRow = [];
    if(!reroll) {
        // only reroll once
        if(components && components.length > 0) {
            buttonRow.push(rowOne);
        }
        if(messy && critical) {
            /* 
                Only available if there is only one Hunger Crit and a maximum of three normal Crits. 
                Will re-roll the normal crits to try and avoid a Messy Critical. 
            */
            let critCount = 0;
            let hungerCritCount = 0;
            for(let i=0; i<dice.length; i++) {
                if(dice[i] == 10) {
                    critCount++;
                }
            }
            for(let i=0; i<hungerDice.length; i++) {
                if(hungerDice[i] == 10) {
                    hungerCritCount++;
                }
            }
            if((critCount >= 1) && (critCount <= 3) && (hungerCritCount <= 1)) {
                buttonRow.push(rowTwo);
            }
        }    
    }

    interaction.reply({
        content: `${[
            hungerBestialMessage, hungerFailMessage, failMessage, 
            successMessage, hungerSuccessMessage, hungerCritMessage, successCritMessage
        ].join('')}`,
        embeds: [ embed ],
        components: buttonRow
    });
}

export const DeleteMusicEmbed = async (guild) => {
    let channelResult = await db.get(`
        SELECT value
        FROM settings
        WHERE guild = ? AND setting = ?
    `, guild.id, "music_channel");

    // do not continue if no channel is set
    if(!channelResult) { return; }

    // validate channel
    let channel = null;
    let channelSnowflakeId = channelResult.value;
    try {
        channel = await guild.channels.fetch(channelSnowflakeId);
    } catch(e) {}

    // do not continue if channel is invalid
    if(!channel) { return; }

    // grab embed id
    let embedResult = await db.get(`
        SELECT value
        FROM settings
        WHERE guild = ? AND setting = ?
    `, guild.id, "music_embed");

    if(embedResult) {
        try {
            let message = await channel.messages.fetch(embedResult.value);
            await message.delete();
        } catch(e) { }
        await db.exec(`
            DELETE FROM settings
            WHERE guild = ? AND setting = ?
        `, guild.id, "music_embed");
    }
}

export const UpdateMusicEmbed = async (guild) => {
    let channelResult = await db.get(`
        SELECT value
        FROM settings
        WHERE guild = ? AND setting = ?
    `, guild.id, "music_channel");

    // do not continue if no channel is set
    if(!channelResult) { return; }

    // validate channel
    let channel = null;
    let channelSnowflakeId = channelResult.value;
    try {
        channel = await guild.channels.fetch(channelSnowflakeId);
    } catch(e) {}

    // do not continue if channel is invalid
    if(!channel) { return; }

    // grab embed id
    let embedResult = await db.get(`
        SELECT value
        FROM settings
        WHERE guild = ? AND setting = ?
    `, guild.id, "music_embed");

    var message = null;
    if(embedResult) {
        try {
            message = await channel.messages.fetch(embedResult.value);
        } catch(e) {
            message = await channel.send({
                embeds: [
                    new EmbedBuilder()
                        .setColor(0xff0000)
                        .setTitle("Not connected...")
                ]
            });
        }
    } else {
        message = await channel.send({
            embeds: [
                new EmbedBuilder()
                    .setColor(0xff0000)
                    .setTitle("Not connected...")
            ]
        });
    }
    
    const voiceConn = getVoiceConnection(guild.id);
    const voiceChannel = voiceConn ? await guild.channels.fetch(voiceConn.joinConfig.channelId) : null;
    const embed = new EmbedBuilder()
        .setColor(voiceConn ? 0x00ff00 : 0xff0000)
        .setTitle(voiceConn ? `Now connected: ${voiceChannel.name}` : "Disconnected...")
        .setTimestamp();
        
    const fileList = [];
    if(voiceConn) {
        let currentQueue = MusicQueue.get(guild.id);
        if(currentQueue) {
            var duration = parseInt(currentQueue.queue[currentQueue.queueIdx].duration);
            var durationMinutes = Math.floor(duration / 60);
            var durationSeconds = Math.floor(duration - durationMinutes * 60);
            embed.addFields([
                { name: 'Now Playing', value: currentQueue.queue[currentQueue.queueIdx].name, inline: false },
                { name: 'Current Playlist', value: currentQueue.playlist, inline: true },
                { name: 'Duration', value: `${String(durationMinutes).padStart(2, '0')}:${String(durationSeconds).padStart(2, '0')}`, inline: true }
            ]);
            embed.addFields([
                { name: 'Volume', value: `${currentQueue.queue[currentQueue.queueIdx].resource.volume.volume * 100}%`, inline: true }
            ]);
            if(currentQueue.startTime) {
                embed.addFields([
                    { name: 'Next Song', value: `<t:${Math.floor(currentQueue.startTime.getTime() / 1000 + duration)}:R>`, inline: true}
                ]);
            }
            
            let playlist = currentQueue.playlist;
            const dir = join(dirname(fileURLToPath(import.meta.url)), "../", "assets");
            if(existsSync(`${dir}/${playlist}/thumb.png`)) {
                fileList.push(new AttachmentBuilder(`${dir}/${playlist}/thumb.png`));
                embed.setThumbnail("attachment://thumb.png");
            }
        } else {
            embed.setDescription("Use the /dismiss command to dismiss me from my voice channel.");
        }
    } else {
        embed.setDescription("Use the /summon command to summon me into your voice channel.");
    }
    message.edit({ embeds: [ embed ], files: fileList });

    await db.run(`
        INSERT OR REPLACE INTO settings
            (guild, setting, value)
        VALUES
            (?, ?, ?)
    `, guild.id, "music_embed", message.id);
}
import { readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import fetch from 'node-fetch';
import dotenv from 'dotenv';
dotenv.config();

// initialize database object
import { db } from './db.js';

// initialize client object
import client from './client.js';

// import button and command actions
import { handleButtonPress } from './buttons.js';
import { handleInputCommand } from './commands.js';
import { UpdateMusicEmbed } from './util.js';

// callbacks
client.once('ready', async () => {
	console.log('Bot Started');
    let guildList = await client.guilds.fetch();
    for(let snowflakePair of guildList.entries()) {
        let [ snowflake, partialGuild ] = snowflakePair;
        partialGuild.fetch()
        .then((guild) => {
            UpdateMusicEmbed(guild);
        });
    }
});

client.on('interactionCreate', async interaction => {
    if(interaction.isButton()) {
        return await handleButtonPress(interaction);
    } else if(interaction.isChatInputCommand()) {
        return await handleInputCommand(interaction);
    } else if (interaction.isAutocomplete()) {
        if(interaction.commandName == "play") {
            const focusedOption = interaction.options.getFocused(true);
            if(focusedOption.name == "playlist") {
                const dir = join(dirname(fileURLToPath(import.meta.url)), "../", "assets");
                const choices = readdirSync(dir);
                const filtered = choices.filter(
                    (item) => item.toLowerCase().includes(focusedOption.value.toLowerCase())
                );
                let final = filtered.map(choice => ({ name: choice, value: choice }))
                if(final.length > 25) {
                    final = final.slice(0, 25);
                }
                await interaction.respond(final);
            }
        }
    } else {
        return;
    }
});

// Login to Discord with your client's token
client.login(process.env.DISCORD_TOKEN);
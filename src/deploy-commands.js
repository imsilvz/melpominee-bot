import { SlashCommandBuilder, Routes } from 'discord.js';
import { REST } from '@discordjs/rest';
import dotenv from 'dotenv';
dotenv.config();

const commands = [
	new SlashCommandBuilder()
		.setName("config")
		.setDescription('Configure bot settings')
		.addSubcommand(subcommand =>
			subcommand
			.setName('get')
			.setDescription('Get Setting')
			.addStringOption(option => 
				option
				.setName("setting")
				.setDescription("Setting Name")
				.setRequired(true)
			))
		.addSubcommand(subcommand =>
			subcommand
			.setName('set')
			.setDescription('Set Setting')
			.addStringOption(option => 
				option
				.setName("setting")
				.setDescription("Setting Name")
				.setRequired(true)
			)
			.addStringOption(option =>
				option
				.setName("args")
				.setDescription("Arguments")
				.setRequired(true)
			)
		),
	new SlashCommandBuilder()
		.setName("v5")
		.setDescription('Roll a VtM dicepool')
		.addNumberOption(option =>
			option
			.setName("pool")
			.setDescription("Number of dice to roll")
			.setRequired(true))
		.addNumberOption(option =>
			option
			.setName("hunger")
			.setDescription("Number of hunger dice in your pool")
			.setRequired(true))
		.addNumberOption(option =>
			option
			.setName("difficulty")
			.setDescription("Difficulty of dice check")
			.setRequired(false)),
	new SlashCommandBuilder()
		.setName("summon")
		.setDescription("Connect to your voice channel"),
	new SlashCommandBuilder()
		.setName("dismiss")
		.setDescription("Disconnect from current voice channel"),
	new SlashCommandBuilder()
		.setName("download")
		.setDescription("Cache audio")
		.addStringOption(option => 
			option
			.setName("video")
			.setDescription("Video URL")
			.setRequired(true)
		)
		.addStringOption(option => 
			option
			.setName("playlist")
			.setDescription("Playlist to add video to")
			.setRequired(true)
		)
		.addBooleanOption(option =>
			option
			.setName("chapters")
			.setDescription("Should the video be split into chapters?")
		)
		.addBooleanOption(option =>
			option
			.setName("thumbnail")
			.setDescription("Should I download the video thumbnail?")
		),
	new SlashCommandBuilder()
		.setName("play")
		.setDescription("Play Music!")
		.addStringOption(option =>
			option
			.setName("playlist")
			.setDescription("Playlist to play")
			.setRequired(true)
			.setAutocomplete(true)
		),
	new SlashCommandBuilder()
		.setName("skip")
		.setDescription("Skip current song!"),
	new SlashCommandBuilder()
		.setName("stop")
		.setDescription("Stop Music!"),
	new SlashCommandBuilder()
		.setName("volume")
		.setDescription("Modify the volume for this playlist")
		.addNumberOption(option =>
			option
			.setName("volume")
			.setDescription("The base volume level. Defaults to 0.05!")
			.setRequired(true)
		)
		.addBooleanOption(option => 
			option
			.setName("song")
			.setDescription("Is this volume specific to the song?")
			.setRequired(false)
		)
].map(command => command.toJSON());

const rest = new REST({ version: '10' }).setToken(process.env.DISCORD_TOKEN);
rest.put(Routes.applicationCommands(process.env.DISCORD_CLIENT), { body: commands })
.then((data) => console.log(`Successfully registered ${data.length} application commands.`))
.catch(console.error);
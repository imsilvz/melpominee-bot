﻿using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
using System.Diagnostics;
using System.Reflection;
namespace Melpominee.Commands
{
    public class PlayCommand : MelpomineeCommand
    {
        public PlayCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "play";
        public override string Description => "Begin streaming a playlist.";

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            var commandGuild = client.Guilds.First((guild) => guild.Id == command.GuildId);
            var playlistName = (string?)command.Data.Options.First().Value;

            if ((commandGuild is null) || (playlistName is null))
            {
                await command.RespondAsync("An error occurred: Invalid guild or playlist name provided.", ephemeral: true);
                return;
            }

            if(await _audioService.StartPlayback(commandGuild, playlistName))
            {
                await command.RespondAsync("Okay!", ephemeral: true);
                return;
            }
            await command.RespondAsync("An error occurred while processing your request.", ephemeral: true);
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("playlist", ApplicationCommandOptionType.String, "Playlist to play", isAutocomplete: true, isRequired: true);
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

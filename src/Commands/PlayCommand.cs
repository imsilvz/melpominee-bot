using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
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
            var commandOpts = command.Data.Options;

            var playlistArg = commandOpts.Where((opt) => opt.Name == "playlist");
            var videoArg = commandOpts.Where((opt) => opt.Name == "url");

            string? playlistName = null;
            string? videoUrl = null;
            if (playlistArg.Count() > 0) { playlistName = (string?)playlistArg.First(); }
            if (videoArg.Count() > 0) { videoUrl = (string?)videoArg.First(); }

            if ((commandGuild is null) || ((playlistName is null) && (videoUrl is null)))
            {
                await command.RespondAsync("An error occurred: Invalid guild or playlist name provided.", ephemeral: true);
                return;
            }

            if ((playlistName is not null) && (videoUrl is not null))
            {
                await command.RespondAsync("An error occured: You must choose either a video URL or a playlist!", ephemeral: true);
                return;
            }

            AudioSource audioSource;
            if(playlistName is not null)
            {
                if (await _audioService.StartPlaylist(commandGuild, playlistName))
                {
                    await command.RespondAsync("Okay!", ephemeral: true);
                    return;
                }
            }
            else if(videoUrl is not null)
            {
                // Regex!
                var rgx1 = Regex.Match(videoUrl, @"youtube\.com\/watch\?v=(.*?)(?:&|$)");
                var rgx2 = Regex.Match(videoUrl, @"youtu\.be\/(.*?)(?:\?|$)");
                if (!(rgx1.Success || rgx2.Success))
                {
                    await command.RespondAsync("Invalid playback URL!", ephemeral: true);
                    return;
                }
                string videoId = rgx1.Success ? rgx1.Groups[1].Value : rgx2.Groups[1].Value;

                audioSource = new AudioSource(AudioSource.SourceType.Networked, videoId);
                _ = Task.Run(async () =>
                {
                    await _audioService.PlayAudio(commandGuild, audioSource);
                });
                await command.RespondAsync("Playback starting!", ephemeral: true);
                return;
            }
            await command.RespondAsync("An error occurred while processing your request.", ephemeral: true);
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("playlist", ApplicationCommandOptionType.String, "Playlist to play", isAutocomplete: true, isRequired: false);
            builder.AddOption("url", ApplicationCommandOptionType.String, "URL to play", isAutocomplete: false, isRequired: false);
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

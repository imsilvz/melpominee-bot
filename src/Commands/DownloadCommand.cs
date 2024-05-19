using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Melpominee.Commands
{
    public class DownloadCommand : MelpomineeCommand
    {
        public override string Name => "download";
        public override string Description => "Download a song for faster playback.";

        public DownloadCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            var commandGuild = client.Guilds.First((guild) => guild.Id == command.GuildId);
            var commandOpts = command.Data.Options;
            var videoArg = commandOpts.Where((opt) => opt.Name == "url");

            string? videoUrl = null;
            if (videoArg.Count() > 0) { videoUrl = (string?)videoArg.First(); }

            if ((commandGuild is null) || (videoUrl is null))
            {
                await command.RespondAsync("An error occurred: Invalid guild or url provided.", ephemeral: true);
                return;
            }

            // Regex!
            var rgx1 = Regex.Match(videoUrl, @"youtube\.com\/watch\?v=(.*?)(?:&|$)");
            var rgx2 = Regex.Match(videoUrl, @"youtu\.be\/(.*?)(?:\?|$)");
            if (!(rgx1.Success || rgx2.Success))
            {
                await command.RespondAsync("Invalid playback URL!", ephemeral: true);
                return;
            }
            string videoId = rgx1.Success ? rgx1.Groups[1].Value : rgx2.Groups[1].Value;

            // defer response until after the download!
            await command.RespondAsync("Download starting...", ephemeral: true);
            _ = Task.Run(async () =>
            {
                var audioSource = new AudioSource(AudioSource.SourceType.Networked, videoId);
                if (await audioSource.Precache())
                {
                    await command.FollowupAsync("Download complete!", ephemeral: true);
                    return;
                }
                await command.FollowupAsync("An error occurred while processing your request.", ephemeral: true);
            });
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("url", ApplicationCommandOptionType.String, "URL to download", isAutocomplete: false, isRequired: true);
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

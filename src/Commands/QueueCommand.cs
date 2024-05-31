using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;
using System.Text.RegularExpressions;
namespace Melpominee.Commands
{
    public class QueueCommand : MelpomineeCommand
    {
        public override string Name => "queue";
        public override string Description => "Enqueue a song for playback";

        public QueueCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }


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
            string parsedVideoUrl = rgx1.Success ? rgx1.Captures[0].Value : rgx2.Captures[0].Value;
            string videoId = rgx1.Success ? rgx1.Groups[1].Value : rgx2.Groups[1].Value;

            _ = Task.Run(async () =>
            {
                var currentChannel = commandGuild.CurrentUser.VoiceChannel;
                if (currentChannel == null)
                {
                    var guildUser = (IGuildUser?)await command.Channel.GetUserAsync(command.User.Id);
                    var voiceChannel = guildUser?.VoiceChannel;
                    if (voiceChannel != null)
                        await _audioService.Connect(voiceChannel);
                }

                var audioSource = new AudioSource(AudioSource.SourceType.Networked, videoId);
                await _audioService.QueueAudio(commandGuild, audioSource);
            });
            await command.RespondAsync($"Audio at `{parsedVideoUrl}` queued for playback!", ephemeral: true);
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("url", ApplicationCommandOptionType.String, "URL of song to enqueue", isAutocomplete: false, isRequired: true);
            builder.WithContextTypes([InteractionContextType.Guild]);
            return builder;
        }
    }
}

using Discord;
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
            var playlistName = (string?)command.Data.Options.Where((opt) => opt.Name == "playlist").FirstOrDefault();
            var videoUrl = (string?)command.Data.Options.Where((opt) => opt.Name == "url").FirstOrDefault();

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
                if (await _audioService.StartVideo(commandGuild, videoUrl))
                {
                    await command.RespondAsync("Playback starting!", ephemeral: true);
                    return;
                }
                else
                {
                    await command.RespondAsync("Link failed regex check!", ephemeral: true);
                    return;
                }
            }
            await command.RespondAsync("An error occurred while processing your request.", ephemeral: true);
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("playlist", ApplicationCommandOptionType.String, "Playlist to play", isAutocomplete: true, isRequired: false);
            builder.AddOption("url", ApplicationCommandOptionType.String, "URL to play", isAutocomplete: true, isRequired: false);
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

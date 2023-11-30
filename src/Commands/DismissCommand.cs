using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
namespace Melpominee.Commands
{
    public class DismissCommand : MelpomineeCommand
    {
        public DismissCommand(AudioFilesystemService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "dismiss";
        public override string Description => "Dismiss Melpominee from all voice channels.";

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            var commandGuild = client.Guilds.First((guild) => guild.Id == command.GuildId);
            var voiceChannel = commandGuild.CurrentUser.VoiceChannel;

            // check if in voice channels
            if (voiceChannel is null)
            {
                await command.RespondAsync("Melpominee is not currently connected to any voice channels!", ephemeral: true);
                return;
            }
            // run as discarded task to avoid blocking
            _ = Task.Run(async () =>
            {
                // connect
                await voiceChannel.DisconnectAsync();
                await command.RespondAsync($"Successfully disconnected from \'{voiceChannel.Name}\'!", ephemeral: true);
            });
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            return builder;
        }
    }
}

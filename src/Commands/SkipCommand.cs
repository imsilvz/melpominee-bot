using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;

namespace Melpominee.src.Commands
{
    public class SkipCommand : MelpomineeCommand
    {
        public SkipCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "skip";
        public override string Description => "Skip current track in the playback queue";

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            var commandGuild = client.Guilds.First((guild) => guild.Id == command.GuildId);

            if (commandGuild is null)
            {
                await command.RespondAsync("An error occurred: Invalid guild.", ephemeral: true);
                return;
            }

            await command.DeferAsync(ephemeral: true);
            _ = Task.Run(async () =>
            {
                if (await _audioService.StopPlayback(commandGuild, false))
                {
                    await command.FollowupAsync("Okay!", ephemeral: true);
                    return;
                }
                await command.FollowupAsync("An error occurred while processing your request.", ephemeral: true);
            });
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

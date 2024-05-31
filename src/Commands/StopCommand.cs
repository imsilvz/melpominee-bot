using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
namespace Melpominee.Commands
{
    public class StopCommand : MelpomineeCommand
    {
        public StopCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "stop";
        public override string Description => "Stop current audio playback!";

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
                if (await _audioService.StopPlayback(commandGuild, true))
                {
                    await command.FollowupAsync("Okay!", ephemeral: true);
                    return;
                }
                await command.FollowupAsync("An error occurred while processing your request.", ephemeral: true);
            });
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.WithContextTypes([InteractionContextType.Guild]);
            return builder;
        }
    }
}

using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
namespace Melpominee.Commands
{
    public class PingCommand : MelpomineeCommand
    {
        public PingCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "ping";

        public override string Description => "This is a Test Command!";

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            await command.RespondAsync("Pong!", ephemeral: true);
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            return builder;
        }
    }
}

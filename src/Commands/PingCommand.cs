using Discord;
using Discord.WebSocket;
using Melpominee.Interfaces;
namespace Melpominee.Commands
{
    public class PingCommand : ISlashCommandHandler
    {

        public string Name => "ping";

        public string Description => "This is a Test Command!";

        public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            await command.RespondAsync("Pong!", ephemeral: true);
        }

        public SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            return builder;
        }
    }
}

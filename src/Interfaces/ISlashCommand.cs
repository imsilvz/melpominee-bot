using Discord;
using Discord.WebSocket;
namespace Melpominee.Interfaces
{
    public interface ISlashCommand
    {
        public string Name { get; }
        public string Description { get; }
        public SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder);
        public Task Execute(DiscordSocketClient client, SocketSlashCommand command);
    }
}

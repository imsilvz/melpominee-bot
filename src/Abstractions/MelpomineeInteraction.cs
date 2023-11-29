using Discord.WebSocket;
namespace Melpominee.Abstractions
{
    public abstract class MelpomineeInteraction
    {
        public virtual string Id => throw new NotImplementedException();
        public abstract Task Execute(DiscordSocketClient client, SocketInteraction interaction);
    }
}

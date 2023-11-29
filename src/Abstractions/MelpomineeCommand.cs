using Discord;
using Discord.WebSocket;
using Melpominee.Services;
namespace Melpominee.Abstractions
{
    public abstract class MelpomineeCommand
    {
        protected DataContext _dataContext;
        public MelpomineeCommand(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public virtual string Name => throw new NotImplementedException();
        public virtual string Description => throw new NotImplementedException();
        public abstract SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder);
        public abstract Task Execute(DiscordSocketClient client, SocketSlashCommand command);
    }
}

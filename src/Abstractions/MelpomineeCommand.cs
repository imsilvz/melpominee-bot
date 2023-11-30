using Discord;
using Discord.WebSocket;
using Melpominee.Services;
namespace Melpominee.Abstractions
{
    public abstract class MelpomineeCommand
    {
        protected AudioFilesystemService _audioService;
        protected DataContext _dataContext;
        public MelpomineeCommand(AudioFilesystemService audioService, DataContext dataContext)
        {
            _audioService = audioService;
            _dataContext = dataContext;
        }

        public virtual string Name => throw new NotImplementedException();
        public virtual string Description => throw new NotImplementedException();
        public abstract SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder);
        public abstract Task Execute(DiscordSocketClient client, SocketSlashCommand command);
    }
}

using Discord.WebSocket;
using Melpominee.Services;
namespace Melpominee.Abstractions
{
    public abstract class MelpomineeInteraction
    {
        protected AudioFilesystemService _audioService;
        protected DataContext _dataContext;
        public MelpomineeInteraction(AudioFilesystemService audioService, DataContext dataContext)
        {
            _audioService = audioService;
            _dataContext = dataContext;
        }

        public virtual string Id => throw new NotImplementedException();
        public abstract Task Execute(DiscordSocketClient client, SocketInteraction interaction);
    }
}

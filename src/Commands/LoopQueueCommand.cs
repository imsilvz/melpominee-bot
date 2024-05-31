using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
namespace Melpominee.Commands
{
    public class LoopQueueCommand : MelpomineeCommand
    {
        public override string Name => "loopqueue";
        public override string Description => "Toggle whether an audio track will be added to the end of the queue upon playback completion.";
        public LoopQueueCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            throw new NotImplementedException();
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("loop", ApplicationCommandOptionType.Boolean, "Enable or disable looping", isRequired: true);
            builder.WithContextTypes([InteractionContextType.Guild]);
            return builder;
        }
    }
}

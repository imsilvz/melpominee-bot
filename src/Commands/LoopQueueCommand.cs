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

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            var commandGuild = client.Guilds.First((guild) => guild.Id == command.GuildId);
            var commandOpts = command.Data.Options;

            bool shouldLoop = false;
            var loopArg = commandOpts.Where((opt) => opt.Name == "loop");
            if (loopArg.Count() > 0) 
            { 
                shouldLoop = (bool)loopArg.First(); 
            }
            else
            {
                await command.RespondAsync("An error occurred: the loop argument is required.", ephemeral: true);
                return;
            }

            if (!_audioService.SetQueueLoop(commandGuild, shouldLoop))
            {
                await command.RespondAsync("An error occurred: am I connected to a channel in this guild?", ephemeral: true);
                return;
            }
            var resultText = shouldLoop ? "now loop." : "no longer loop.";
            await command.RespondAsync($"Audio queue will {resultText}", ephemeral: true);
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("loop", ApplicationCommandOptionType.Boolean, "Enable or disable looping", isRequired: true);
            builder.WithContextTypes([InteractionContextType.Guild]);
            return builder;
        }
    }
}

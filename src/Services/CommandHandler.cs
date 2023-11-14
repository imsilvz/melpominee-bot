using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Melpominee.Interfaces;
namespace Melpominee.Services
{
    public class CommandHandler : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, ISlashCommand> _commandCache;
        public CommandHandler(DiscordSocketClient client) 
        {
            _client = client;
            _commandCache = new Dictionary<string, ISlashCommand>();
        }

        public async Task InstallCommands()
        {
            // fetch all classes implementing ISlashCommand
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISlashCommand).IsAssignableFrom(p) && p.IsClass);

            // create instance and register command with bot
            var commandList = new List<ApplicationCommandProperties>();
            foreach (var type in types) 
            {
                // create instance
                var instance = (ISlashCommand)Activator.CreateInstance(type)!;

                // build command with known properties, then allow custom configuration
                var commandBuilder = new SlashCommandBuilder();
                commandBuilder.WithName(instance.Name);
                commandBuilder.WithDescription(instance.Description);
                instance.Register(_client, commandBuilder);

                // add the command to the commandlist, as well as cache the handler object
                commandList.Add(commandBuilder.Build());
                _commandCache.Add(instance.Name, instance);
            }
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(commandList.ToArray());
        }

        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            var commandName = command.Data.Name;
            if (_commandCache.TryGetValue(commandName, out var instance))
            {
                await instance.Execute(_client, command);
            }
            else
            {
                await command.RespondAsync($"Unable to locate handler for '/{command.Data.Name}'!");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.Ready += InstallCommands;
            _client.SlashCommandExecuted += SlashCommandHandler;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

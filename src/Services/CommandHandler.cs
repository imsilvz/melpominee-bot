using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Melpominee.Abstractions;
using Microsoft.Extensions.Logging;
namespace Melpominee.Services
{
    public class CommandHandler : IHostedService
    {
        private readonly ILogger<DiscordSocketClient> _logger;
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, MelpomineeCommand> _commandCache;
        private readonly AudioService _audioService;
        private readonly DataContext _dataContext;
        public CommandHandler(ILogger<DiscordSocketClient> logger, DiscordSocketClient client, AudioService audioService, DataContext dataContext) 
        {
            _logger = logger;
            _client = client;
            _commandCache = new Dictionary<string, MelpomineeCommand>();
            _audioService = audioService;
            _dataContext = dataContext;
        }

        public async Task InstallCommands()
        {
            // fetch all classes implementing ISlashCommand
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(MelpomineeCommand).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

            // create instance and register command with bot
            var commandList = new List<ApplicationCommandProperties>();
            foreach (var type in types) 
            {
                // create instance
                var instance = (MelpomineeCommand)Activator.CreateInstance(type, new object[] { _audioService, _dataContext })!;

                // build command with known properties, then allow custom configuration
                var commandBuilder = new SlashCommandBuilder();
                commandBuilder.WithName(instance.Name);
                commandBuilder.WithDescription(instance.Description);
                instance.Register(_client, commandBuilder);

                // add the command to the commandlist, as well as cache the handler object
                commandList.Add(commandBuilder.Build());
                _commandCache.Add(instance.Name, instance);
            }
            //await _client.BulkOverwriteGlobalApplicationCommandsAsync(commandList.ToArray());
        }

        public async Task SlashCommandHandler(SocketSlashCommand command)
        {
            var commandName = command.Data.Name;
            if (_commandCache.TryGetValue(commandName, out var instance))
            {
                // run all commands async
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await instance.Execute(_client, command);
                    }
                    catch (Exception ex) 
                    {
                        if (command.HasResponded)
                        {
                            await command.FollowupAsync($"An error occurred while executing the \'{commandName}\' command.", ephemeral: true);
                        }
                        _logger.LogError(new LogMessage(LogSeverity.Error, "Command", $"An error occurred while executing the \'{commandName}\' command", ex).ToString());
                    }
                });
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

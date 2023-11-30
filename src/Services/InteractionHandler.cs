using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Microsoft.Extensions.Hosting;
namespace Melpominee.Services
{
    public class InteractionHandler : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, MelpomineeInteraction> _interactionCache;
        private readonly AudioFilesystemService _audioService;
        private readonly DataContext _dataContext;
        public InteractionHandler(DiscordSocketClient client, AudioFilesystemService audioService, DataContext dataContext)
        {
            _client = client;
            _interactionCache = new Dictionary<string, MelpomineeInteraction>();
            _audioService = audioService;
            _dataContext = dataContext;
        }

        public Task InstallInteractions()
        {
            // fetch all classes implementing ISlashCommand
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(MelpomineeInteraction).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

            // create instance and register command with bot
            var commandList = new List<ApplicationCommandProperties>();
            foreach (var type in types)
            {
                // create instance
                var instance = (MelpomineeInteraction)Activator.CreateInstance(type, new object[] { _audioService, _dataContext })!;
                _interactionCache.Add(instance.Id, instance);
            }
            return Task.CompletedTask;
        }

        public async Task HandleInteraction(SocketInteraction inter)
        {
            if (inter.Type == InteractionType.ApplicationCommandAutocomplete)
            {
                MelpomineeInteraction? handler;
                var component = (SocketAutocompleteInteraction)inter;
                var commandId = $"{component.Data.CommandName}-{component.Data.Current.Name}";
                if (!_interactionCache.TryGetValue(commandId, out handler))
                    throw new Exception($"Unable to locate autocomplete handler for '/{commandId}'!");
                await handler.Execute(_client, inter);
            }
            else if (inter.Type == InteractionType.MessageComponent)
            {
                MelpomineeInteraction? handler;
                var component = (SocketMessageComponent) inter;
                var customId = component.Data.CustomId;
                if (!_interactionCache.TryGetValue(customId, out handler))
                    throw new Exception($"Unable to locate interaction handler for '/{customId}'!");
                await handler.Execute(_client, inter);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.Ready += InstallInteractions;
            _client.InteractionCreated += HandleInteraction;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

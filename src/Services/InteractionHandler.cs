using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Melpominee.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Melpominee.Services
{
    public class InteractionHandler : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<string, IInteractionHandler> _interactionCache;
        public InteractionHandler(DiscordSocketClient client)
        {
            _client = client;
            _interactionCache = new Dictionary<string, IInteractionHandler>();
        }

        public Task InstallInteractions()
        {
            // fetch all classes implementing ISlashCommand
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(IInteractionHandler).IsAssignableFrom(p) && p.IsClass);

            // create instance and register command with bot
            var commandList = new List<ApplicationCommandProperties>();
            foreach (var type in types)
            {
                // create instance
                var instance = (IInteractionHandler)Activator.CreateInstance(type)!;
                _interactionCache.Add(instance.Id, instance);
            }
            return Task.CompletedTask;
        }

        public async Task HandleInteraction(SocketInteraction inter)
        {
            if (inter.Type == InteractionType.MessageComponent)
            {
                IInteractionHandler handler;
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

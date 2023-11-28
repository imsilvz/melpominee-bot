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
        private readonly Dictionary<string, IDiscordInteraction> _interactionCache;
        public InteractionHandler(DiscordSocketClient client)
        {
            _client = client;
            _interactionCache = new Dictionary<string, IDiscordInteraction>();
        }

        public Task InstallInteractions()
        {
            // fetch all classes implementing ISlashCommand
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => typeof(ISlashCommand).IsAssignableFrom(p) && p.IsClass);
            return Task.CompletedTask;
        }

        public async Task ButtonInteractionHandler(SocketMessageComponent component)
        {
            Console.WriteLine(component.Data.CustomId);
            Console.WriteLine(component.Data.Type);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.Ready += InstallInteractions;
            _client.ButtonExecuted += ButtonInteractionHandler;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

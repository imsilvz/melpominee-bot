using Discord.WebSocket;
using Melpominee.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace Melpominee
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            //await DataContext.Instance.Initialize();
            using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<AudioService>();
                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton((p) => DataContext.Instance);
                services.AddHostedService((p) => p.GetRequiredService<AudioService>());
                services.AddHostedService<CommandHandler>();
                services.AddHostedService<InteractionHandler>();
                services.AddHostedService<MelpomineeService>();
            })
            .Build();
            await host.RunAsync();
        }
    }
}
// See https://aka.ms/new-console-template for more information
using Discord;
using Discord.Commands;
using Discord.Interactions;
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
            using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton<InteractionService>();
                services.AddHostedService<CommandHandler>();
                services.AddHostedService<MelpomineeService>();
            })
            .Build();
            await host.RunAsync();

        }
    }
}
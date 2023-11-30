﻿using Discord.WebSocket;
using Melpominee.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using Azure.Identity;
namespace Melpominee
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<DataContext>();
                services.AddSingleton<DiscordSocketClient>();
                services.AddHostedService<AudioFilesystemService>();
                services.AddHostedService<CommandHandler>();
                services.AddHostedService<InteractionHandler>();
                services.AddHostedService<MelpomineeService>();
            })
            .Build();
            await host.RunAsync();
        }
    }
}
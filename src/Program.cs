﻿using NetCord.Services.ApplicationCommands;
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
                //services.AddSingleton((p) => DataContext.Instance);
                services.AddSingleton<MelpomineeAudioService>();
                services.AddHostedService<MelpomineeService>();
            })
            .Build();
            await host.RunAsync();
        }
    }
}
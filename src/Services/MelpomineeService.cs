﻿using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Logging;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;

namespace Melpominee.Services
{
    public class MelpomineeService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        // melpominee
        private readonly MelpomineeAudioService _audioService;

        // discord
        private readonly ShardedGatewayClient _gateway;
        private readonly ApplicationCommandService<ApplicationCommandContext> _commandService;
        private readonly ComponentInteractionService<ButtonInteractionContext> _buttonInteractionService;
        
        public MelpomineeService(IServiceProvider serviceProvider, MelpomineeAudioService audioService)
        {
            var assembly = typeof(Program).Assembly;
            _serviceProvider = serviceProvider;

            // setup melpominee audio
            _audioService = audioService;

            // startup bot
            _gateway = new ShardedGatewayClient(
                new BotToken(SecretStore.Instance.GetSecret("DISCORD_TOKEN")),
                new ShardedGatewayClientConfiguration
                {
                    ShardCount = 1, // Adjust as needed
                    LoggerFactory = ShardedConsoleLogger.GetFactory(),
                }
            );

            // Create application command services
            _commandService = new();

            // Add command interactions from modules
            _commandService.AddModules(assembly);

            // Create interaction services
            _buttonInteractionService = new();

            // Add component interactions from modules
            _buttonInteractionService.AddModules(assembly);

            // Setup Gateway Events
            _gateway.VoiceStateUpdate += _audioService.VoiceStateUpdated;
            _gateway.InteractionCreate += async (gateway, interaction) =>
            {
                // Execute the command
                var result = await (interaction switch
                {
                    ApplicationCommandInteraction applicationCommandInteraction => _commandService.ExecuteAsync(
                        new ApplicationCommandContext(applicationCommandInteraction, gateway),
                        _serviceProvider
                    ),
                    ButtonInteraction buttonInteraction => _buttonInteractionService.ExecuteAsync(
                        new ButtonInteractionContext(buttonInteraction, gateway),
                        _serviceProvider
                    ),
                    _ => throw new("Invalid interaction."),
                });

                // Check if the execution failed
                if (result is not IFailResult failResult)
                    return;

                // Return the error message to the user if the execution failed
                try
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message(failResult.Message));
                }
                catch
                {

                }
            };

            _gateway.Ready += OnClientReady;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create the commands so that you can use them in the Discord client
            await _commandService.CreateCommandsAsync(_gateway.Rest, _gateway.Id);
            await _gateway.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _gateway.CloseAsync();
        }

        private ValueTask OnClientReady(GatewayClient client, ReadyEventArgs args)
        {
            return ValueTask.CompletedTask;
        }
    }
}

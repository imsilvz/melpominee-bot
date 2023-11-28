using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Melpominee.Services
{
    public class MelpomineeService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly ILogger<DiscordSocketClient> _logger;
        public MelpomineeService(DiscordSocketClient client, ILogger<DiscordSocketClient> logger)
        {
            // startup bot
            _client = client;
            _logger = logger;

            _client.Log += OnDiscordLog;
            _client.Ready += OnClientReady;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _client.LoginAsync(TokenType.Bot, SecretStore.Instance.GetSecret("DISCORD_TOKEN"));
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private Task OnClientReady()
        {
            return Task.CompletedTask;
        }

        private Task OnDiscordLog(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Verbose:
                    _logger.LogInformation(msg.ToString());
                    break;

                case LogSeverity.Info:
                    _logger.LogInformation(msg.ToString());
                    break;

                case LogSeverity.Warning:
                    _logger.LogWarning(msg.ToString());
                    break;

                case LogSeverity.Error:
                    _logger.LogError(msg.ToString());
                    break;

                case LogSeverity.Critical:
                    _logger.LogCritical(msg.ToString());
                    break;
            }
            return Task.CompletedTask;
        }
    }
}

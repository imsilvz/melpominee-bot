using Discord;
using Discord.Audio;
using Melpominee.Services;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading;
namespace Melpominee.Models
{
    public class AudioConnection : IDisposable
    {
        public IAudioClient? Client { get; set; }
        public IVoiceChannel Channel { get; set; }
        public IGuild Guild { get; set; }
        public bool LoopQueue { get; set; } = false;
        public ConcurrentQueue<AudioSource> AudioQueue { get; set; }
        public AudioService.PlaybackStatus PlaybackStatus { get; set; } = AudioService.PlaybackStatus.Unknown;
        public AudioOutStream? DiscordPCMStream { get; set; }
        public CancellationTokenSource PlaybackCancellationToken { get; set; }

        public AudioConnection(IVoiceChannel channel)
        {
            Channel = channel;
            Guild = channel.Guild;

            AudioQueue = new ConcurrentQueue<AudioSource>();
            PlaybackStatus = AudioService.PlaybackStatus.Idle;
            PlaybackCancellationToken = new CancellationTokenSource();
        }

        public async Task Connect(bool deaf, bool mute)
        {
            var audioClient = await Channel.ConnectAsync(deaf, mute, false);
            audioClient.Connected += OnConnected;
            audioClient.Disconnected += OnDisconnected;
            DiscordPCMStream = audioClient.CreatePCMStream(
                AudioApplication.Music,
                bitrate: 128 * 1024,
                bufferMillis: 250,
                packetLoss: 40
            );
        }

        public Task OnConnected()
        {
            Console.WriteLine($"Connected: {Guild.Id}");
            return Task.CompletedTask;
        }

        public async Task Disconnect()
        {
            if (Client is not null)
                await Client.StopAsync();
            Dispose();
        }

        public async Task OnDisconnected(Exception e)
        {
            Console.WriteLine($"Disconnected: {Guild.Id}");
            // TaskCancelledException points to an intentional disconnect
            if (e is TaskCanceledException) 
            { Console.WriteLine("TASK CANCELLED!"); }
            else
            {
                // Attempt reconnect!
                var currentUser = await Guild.GetCurrentUserAsync();
                Console.WriteLine($"{Channel.Id} -> {currentUser.VoiceChannel.Id}");
            }
        }

        public void Dispose()
        {
            PlaybackCancellationToken.Dispose();
            Client?.Dispose();
            DiscordPCMStream?.Dispose();
        }
    }
}

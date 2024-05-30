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
        public CancellationTokenSource PlaybackCancellationToken { get; set; }

        public AudioConnection(IVoiceChannel channel)
        {
            Channel = channel;
            Guild = channel.Guild;

            AudioQueue = new ConcurrentQueue<AudioSource>();
            PlaybackStatus = AudioService.PlaybackStatus.Idle;
            PlaybackCancellationToken = new CancellationTokenSource();
        }

        public async Task Connect(bool deaf = true, bool mute = false)
        {
            Client = await Channel.ConnectAsync(deaf, mute, false);
            PlaybackCancellationToken = new CancellationTokenSource();
        }

        public async Task Disconnect()
        {
            if (Client is not null)
                await Client.StopAsync();
            Dispose();
        }

        public async Task EnsureConnection()
        {
            if (
                Client == null || 
                Client.ConnectionState == ConnectionState.Disconnected
                )
            {
                Dispose();
                await Connect();
            }
        }

        public AudioOutStream? GetDiscordStream()
        {
            return Client?.CreatePCMStream(
                AudioApplication.Music, 
                bitrate: 128 * 1024, 
                bufferMillis: 250, 
                packetLoss: 40
            );
        }

        public void Dispose()
        {
            PlaybackCancellationToken.Dispose();
            Client?.Dispose();
        }
    }
}

using Discord;
using Discord.Audio;
using Melpominee.Services;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading;
namespace Melpominee.Models
{
    public class AudioConnection
    {
        public required IAudioClient Client { get; set; }
        public required IVoiceChannel Channel { get; set; }
        public required IGuild Guild { get; set; }
        public required ConcurrentQueue<AudioSource> AudioQueue { get; set; }
        public AudioService.PlaybackStatus PlaybackStatus { get; set; } = AudioService.PlaybackStatus.Unknown;
        public required CancellationTokenSource PlaybackCancellationToken { get; set; }
        public async Task Reconnect()
        {
            var audioClient = await Channel.ConnectAsync(true, false, false);
            audioClient.Connected += OnConnected;
            audioClient.Disconnected += OnDisconnected;
            Client = audioClient;
        }

        public Task OnConnected()
        {
            Console.WriteLine($"Connected: {Guild.Id}");
            return Task.CompletedTask;
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
                /*if (Channel.Id != currentUser.VoiceChannel.Id)
                {
                    // bot has moved channels!
                    Channel = currentUser.VoiceChannel;
                }
                await Task.Delay(250);
                await Reconnect();*/
            }
        }
    }
}

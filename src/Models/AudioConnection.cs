using Discord;
using Discord.Audio;
using Melpominee.Services;
using System.Collections.Concurrent;
namespace Melpominee.Models
{
    public class AudioConnection
    {
        public required IAudioClient Client { get; set; }
        public required IVoiceChannel Channel { get; set; }
        public required IGuild Guild { get; set; }
        public required ConcurrentQueue<AudioSource> AudioQueue { get; set; }
        public AudioService.PlaybackStatus PlaybackStatus { get; set; } = AudioService.PlaybackStatus.Unknown;
        public required CancellationTokenSource playbackCancellationToken { get; set; }
    }
}

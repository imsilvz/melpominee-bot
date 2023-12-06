using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Melpominee.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Melpominee.Models
{
    public class AudioConnection
    {
        public required IAudioClient Client { get; set; }
        public required IVoiceChannel Channel { get; set; }
        public required IGuild Guild { get; set; }
        public AudioService.PlaybackStatus PlaybackStatus { get; set; } = AudioService.PlaybackStatus.Unknown;
        public required CancellationTokenSource playbackCancellationToken { get; set; }
    }
}

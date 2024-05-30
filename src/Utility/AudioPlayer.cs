using Discord;
using Discord.Audio;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Discord.Audio.Streams;
using Melpominee.Models;
namespace Melpominee.Utility
{
    public class AudioPlayer
    {
        public event EventHandler<AudioSource>? PlaybackFinished;

        private AudioConnection _conn;
        public AudioPlayer(AudioConnection conn) 
        { 
            _conn = conn;
        }

        public async Task PlaySource(AudioSource source)
        {
            // update playback status
            _conn.Status = AudioConnection.PlaybackStatus.Playing;

            // Setup cancellation token to stop if needs be
            var cancellationTokenSource = _conn.PlaybackCancellationToken;
            if (cancellationTokenSource.IsCancellationRequested || !cancellationTokenSource.TryReset())
            {
                cancellationTokenSource = new CancellationTokenSource();
                _conn.PlaybackCancellationToken = cancellationTokenSource;
            }
            var cancellationToken = cancellationTokenSource.Token;

            // begin playback
            var client = _conn.Client;
            var fileStream = source.GetStream();
            using (var discordStream = _conn.GetDiscordStream())
            {
                try
                {
                    int bufferSize = 1024;
                    byte[] readBuffer = new byte[bufferSize];
                    byte[] writeBuffer = new byte[bufferSize];
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if ((fileStream is null) || (discordStream is null))
                            break;
                        int bytesRead = await fileStream.ReadAsync(readBuffer, 0, bufferSize);
                        if (bytesRead <= 0)
                        {
                            break;
                        }
                        else
                        {
                            Buffer.BlockCopy(readBuffer, 0, writeBuffer, 0, bytesRead);
                            if (discordStream != null)
                                await discordStream.WriteAsync(writeBuffer, 0, bytesRead);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _conn.Status = AudioConnection.PlaybackStatus.Cancelled;
                }
                finally
                {
                    if (discordStream != null)
                        await discordStream.FlushAsync();
                    // fire event handler
                    PlaybackFinished?.Invoke(this, source);
                }
            }
        }
    }
}

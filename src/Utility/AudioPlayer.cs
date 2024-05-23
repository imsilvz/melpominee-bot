using Discord;
using Discord.Audio;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Discord.Audio.Streams;
using Melpominee.Models;
using static Melpominee.Services.AudioService;
namespace Melpominee.Utility
{
    public class AudioPlayer
    {
        public event EventHandler<AudioConnection>? PlaybackFinished;
        public async Task StartPlayback(AudioConnection conn, AudioSource source)
        {
            // update playback status
            conn.PlaybackStatus = PlaybackStatus.Playing;

            // Setup cancellation token to stop if needs be
            var cancellationTokenSource = conn.PlaybackCancellationToken;
            if (cancellationTokenSource.IsCancellationRequested || !cancellationTokenSource.TryReset())
            {
                cancellationTokenSource = new CancellationTokenSource();
                conn.PlaybackCancellationToken = cancellationTokenSource;
            }
            var cancellationToken = cancellationTokenSource.Token;

            // begin playback
            var client = conn.Client;
            var fileStream = source.GetStream();
            try
            {
                int bufferSize = 1024;
                byte[] readBuffer = new byte[bufferSize];
                byte[] writeBuffer = new byte[bufferSize];
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (fileStream is null)
                        break;
                    int bytesRead = await fileStream.ReadAsync(readBuffer, 0, bufferSize, cancellationToken);
                    if (bytesRead <= 0)
                    {
                        break;
                    }
                    else
                    {
                        Buffer.BlockCopy(readBuffer, 0, writeBuffer, 0, bytesRead);
                        if (conn.DiscordPCMStream != null)
                            await conn.DiscordPCMStream.WriteAsync(writeBuffer, 0, bytesRead, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            finally
            {
                if (conn.DiscordPCMStream != null)
                    await conn.DiscordPCMStream.FlushAsync();
                // fire event handler
                _ = Task.Run(() =>
                {
                    PlaybackFinished?.Invoke(this, conn);
                });
            }
        }
    }
}

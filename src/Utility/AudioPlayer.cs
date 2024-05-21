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
        public async Task StartPlayback(AudioConnection conn, AudioSource source, CancellationToken cancellationToken = default(CancellationToken))
        {
            conn.PlaybackStatus = PlaybackStatus.Playing;

            var client = conn.Client;
            var fileStream = source.GetStream();
            using (var discordStream = client.CreatePCMStream(AudioApplication.Music, bitrate: 128 * 1024, bufferMillis: 250, packetLoss: 40))
            {
                int bufferSize = 1024;
                byte[] readBuffer = new byte[bufferSize];
                byte[] writeBuffer = new byte[bufferSize];

                try
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (discordStream is null)
                            throw new Exception("Discord stream is has been broken!");

                        if (fileStream is null) break;
                        int bytesRead = await fileStream.ReadAsync(readBuffer, 0, bufferSize, cancellationToken);
                        if (bytesRead <= 0)
                        {
                            break;
                        }
                        else
                        {
                            Buffer.BlockCopy(readBuffer, 0, writeBuffer, 0, bytesRead);
                            await discordStream.WriteAsync(writeBuffer, 0, bytesRead, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                finally
                {
                    await discordStream.FlushAsync();

                    // fire event handler
                    _ = Task.Run(() =>
                    {
                        PlaybackFinished?.Invoke(this, conn);
                    });
                }
            }
        }
    }
}

using Discord;
using Discord.Audio;
using System.Diagnostics;
using static Melpominee.Services.AudioService;
using System.IO;
using System.Threading;
using Discord.Audio.Streams;
namespace Melpominee.Utility
{
    public class AudioPlayer
    {
        public async Task StartPlayback(IAudioClient client, Stream fileStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var discordStream = client.CreatePCMStream(AudioApplication.Music))
            {
                int bufferSize = 1024;
                byte[] readBuffer = new byte[bufferSize];
                byte[] writeBuffer = new byte[bufferSize];

                try
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (fileStream is null || discordStream is null) break;
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
                catch (OperationCanceledException) { }
                finally
                {
                    await discordStream.FlushAsync();
                }
            }
        }
    }
}

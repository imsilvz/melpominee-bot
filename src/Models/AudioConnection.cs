using Discord;
using Discord.Audio;
using System.Collections.Concurrent;
using Melpominee.Utility;
using Discord.WebSocket;
using System;
namespace Melpominee.Models
{
    public class AudioConnection : IDisposable
    {
        public enum PlaybackStatus
        {
            Unknown = 0,
            Idle = 1,
            Playing = 2,
            Cancelled = 3,
            Error = 4,
        }

        public IAudioClient? Client { get; set; }
        public IVoiceChannel Channel { get; set; }
        public IGuild Guild { get; set; }
        public bool LoopQueue { get; set; } = false;
        public ConcurrentQueue<AudioSource> AudioQueue { get; set; }
        public PlaybackStatus Status { get; set; } = PlaybackStatus.Unknown;
        public CancellationTokenSource PlaybackCancellationToken { get; set; }

        private AudioPlayer _player;
        public AudioConnection(IVoiceChannel channel)
        {
            Channel = channel;
            Guild = channel.Guild;

            AudioQueue = new ConcurrentQueue<AudioSource>();
            Status = PlaybackStatus.Idle;
            PlaybackCancellationToken = new CancellationTokenSource();

            _player = new AudioPlayer(this);
            _player.PlaybackFinished += QueueHandler;
        }

        /* Connection Methods */
        public async Task Connect(bool deaf = true, bool mute = false)
        {
            Client = await Channel.ConnectAsync(deaf, mute, false);
            PlaybackCancellationToken = new CancellationTokenSource();
            Status = PlaybackStatus.Idle;
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

        /* Playback Methods */
        public void ClearAudioQueue()
        {
            AudioQueue.Clear();
        }

        public async Task StartPlayback()
        {
            await EnsureConnection();

            if (Status != PlaybackStatus.Idle)
                return;
            QueueHandler(this, null);
        }

        public async Task QueueSource(AudioSource source, bool waitForCache = true)
        {
            Task cacheTask;
            if (!source.GetCached())
                cacheTask = source.Precache();
            else
                cacheTask = Task.CompletedTask;
            AudioQueue.Enqueue(source);
            if (waitForCache)
            {
                await cacheTask;
            }
            else
            {
                _ = Task.Run(async () => { await cacheTask; });
            }
        }

        public async Task StopPlayback(bool waitForIdle = false)
        {
            LoopQueue = false;
            PlaybackCancellationToken.Cancel();
            if (waitForIdle)
            {
                while (GetConnected())
                {
                    if (!GetAudioPlaying()) break;
                    await Task.Delay(1);
                }
            }
        }

        /* Getters */
        public bool GetConnected()
        {
            return !(Client == null || Client.ConnectionState == ConnectionState.Disconnected);
        }

        public bool GetAudioPlaying()
        {
            return Status != PlaybackStatus.Idle;
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

        /* Misc */
        public void QueueHandler(object? sender, AudioSource? prevSource)
        {
            if (AudioQueue.TryDequeue(out var audioSource))
            {
                // 
                if (prevSource != null && LoopQueue)
                {
                    QueueSource(
                        new AudioSource(
                            prevSource.GetSourceType(),
                            prevSource.GetSource()
                        ),
                        false
                    ).ConfigureAwait(false);
                }

                Status = PlaybackStatus.Playing;
                _ = Task.Run(async () =>
                {
                    using(audioSource) 
                    { 
                        await _player.PlaySource(audioSource);
                    }
                });
            }
            else
            {
                Status = PlaybackStatus.Idle;
            }
        }

        public void Dispose()
        {
            PlaybackCancellationToken.Cancel();
            PlaybackCancellationToken.Dispose();
            Client?.Dispose();
        }
    }
}

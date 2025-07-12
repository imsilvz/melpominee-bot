using NetCord.Gateway.Voice;
using System.IO;
using System.IO.Pipes;
using System.Threading;

using Melpominee.Utility;
namespace Melpominee.Models;

public class VoiceInstance : IDisposable
{
    public enum PlaybackStatus
    {
        Error = -1,
        Unknown = 0,
        Idle = 1,
        Playing = 2,
        Paused = 3,
        Cancelled = 4,
        Stopped = 5,
    }

    private event EventHandler<AudioSource>? AudioFinished;

    private VoiceClient _voiceClient;

    private LinkedList<AudioSource> _audioQueue = new();
    private SemaphoreSlim _audioQueueSemaphore = new(1);

    private PlaybackStatus _playbackStatus = PlaybackStatus.Idle;
    private SemaphoreQueue _audioStateSemaphore = new(1);
    public VoiceInstance(VoiceClient voiceClient)
    {
        _voiceClient = voiceClient;
        AudioFinished += OnAudioFinished;
    }

    public void Dispose()
    {
        _audioQueue.Clear();
        _audioQueueSemaphore.Dispose();
        _voiceClient.Dispose();
    }

    public async Task StartPlayback()
    {
        await _audioStateSemaphore.WaitAsync();
        try
        {
            if (_playbackStatus != PlaybackStatus.Playing)
            {
                if (_playbackStatus == PlaybackStatus.Idle)
                {
                    _playbackStatus = PlaybackStatus.Playing;
                    _ = Task.Run(async () => await PlayNext());
                } else if (_playbackStatus != PlaybackStatus.Cancelled)
                {
                    _playbackStatus = PlaybackStatus.Playing;
                }
            }
        }
        finally
        {
            _audioStateSemaphore.Release();
        }
    }

    public async Task PausePlayback()
    {
        await _audioStateSemaphore.WaitAsync();
        try
        {
            if (_playbackStatus == PlaybackStatus.Playing)
            {
                _playbackStatus = PlaybackStatus.Paused;
            }
        }
        finally
        {
            _audioStateSemaphore.Release();
        }
    }

    public async Task StopPlayback()
    {
        await _audioStateSemaphore.WaitAsync();
        try
        {
            _playbackStatus = PlaybackStatus.Stopped;
        }
        finally
        {
            _audioStateSemaphore.Release();
        }
    }

    public async Task<bool> QueueAudio(AudioSource source, bool addToStart = false)
    {
        await _audioQueueSemaphore.WaitAsync();
        try
        {
            if (addToStart)
                _audioQueue.AddFirst(source);
            else
                _audioQueue.AddLast(source);
        }
        finally
        {
            _audioQueueSemaphore.Release();
        }
        return true;
    }

    public async Task<bool> SkipAudio()
    {
        await _audioQueueSemaphore.WaitAsync();
        try
        {
            if (_playbackStatus == PlaybackStatus.Playing || _playbackStatus == PlaybackStatus.Paused)
            _playbackStatus = PlaybackStatus.Cancelled;
        }
        finally
        {
            _audioQueueSemaphore.Release();
        }
        return true;
    }

    public async Task ClearQueue()
    {
        await _audioQueueSemaphore.WaitAsync();
        try
        {
            _audioQueue.Clear();
        }
        finally
        {
            _audioQueueSemaphore.Release();
        }
    }

    public async Task<int> GetQueueLength()
    {
        await _audioQueueSemaphore.WaitAsync();
        try
        {
            return _audioQueue.Count;
        }
        finally
        {
            _audioQueueSemaphore.Release();
        }
    }

    public async Task<PlaybackStatus> GetPlaybackState()
    {
        await _audioStateSemaphore.WaitAsync();
        try
        {
            return _playbackStatus;
        }
        finally
        {
            _audioStateSemaphore.Release();
        }
    }

    private async Task PlayNext()
    {
        AudioSource source;
        await Task.WhenAll(
            _audioStateSemaphore.WaitAsync(),
            _audioQueueSemaphore.WaitAsync()
        );
        try
        {
            if (_audioQueue.Count <= 0)
            {
                _playbackStatus = PlaybackStatus.Idle;
                return;
            }
            source = _audioQueue.First();
            _audioQueue.RemoveFirst();
        }
        finally
        {
            _audioStateSemaphore.Release();
            _audioQueueSemaphore.Release();
        }

        // Enter speaking state, to be able to send voice
        Console.WriteLine("Entering speaking state...");
        await _voiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone));

        // Create a stream that sends voice to Discord
        Console.WriteLine("Creating streams...");
        using (var outStream = _voiceClient.CreateOutputStream(normalizeSpeed: true))
        using (OpusEncodeStream opusStream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio))
        using (var fileStream = source.GetStream())
        {
            // We create this stream to automatically convert the PCM data returned by FFmpeg to Opus data.
            // The Opus data is then written to 'outStream' that sends the data to Discord
            // Copy the FFmpeg stdout to 'opusStream', which encodes the voice using Opus and passes it to 'outStream'
            Console.WriteLine("Copying audio source to output stream...");
            try
            {
                bool flushed = false;
                int bufferSize = 2048;
                byte[] readBuffer = new byte[bufferSize];
                byte[] writeBuffer = new byte[bufferSize];
                while (true)
                {
                    _audioStateSemaphore.Wait();
                    try
                    {
                        if (_playbackStatus == PlaybackStatus.Paused)
                        {
                            if (!flushed)
                            {
                                await opusStream.FlushAsync();
                                flushed = true;
                            }
                            await Task.Delay(100); // Wait for 1 second before checking again
                            continue;
                        } else if (_playbackStatus == PlaybackStatus.Cancelled || _playbackStatus == PlaybackStatus.Stopped)
                        {
                            // break out of playback loop
                            await opusStream.FlushAsync();
                            break;
                        }

                        if ((fileStream is null) || (opusStream is null))
                            break;
                        int bytesRead = await fileStream.ReadAsync(readBuffer, 0, bufferSize);
                        if (bytesRead <= 0)
                        {
                            break;
                        }
                        else
                        {
                            Buffer.BlockCopy(readBuffer, 0, writeBuffer, 0, bytesRead);
                            if (opusStream != null)
                            {
                                await opusStream.WriteAsync(writeBuffer, 0, bytesRead);
                                flushed = false;
                            }
                        }
                    }
                    finally
                    {
                        _audioStateSemaphore.Release();
                    }
                }
            }
            finally
            {
                if (opusStream != null)
                {
                    // Flush 'opusStream' to make sure all the data has been sent and to indicate to Discord that we have finished sending
                    Console.WriteLine("Flushing output stream...");
                    await opusStream.FlushAsync();
                }
                AudioFinished?.Invoke(this, source);
            }
        }
    }

    private void OnAudioFinished(object? voiceInstance, AudioSource completedSource)
    {
        Task.WhenAll(
            _audioStateSemaphore.WaitAsync(),
            _audioQueueSemaphore.WaitAsync()
        ).Wait();
        try
        {
            if (_playbackStatus == PlaybackStatus.Stopped)
            {
                Console.WriteLine("Audio finished playing with status Stopped, breaking out of loop...");
                _playbackStatus = PlaybackStatus.Idle;
                return;
            }
            Console.WriteLine("Audio finished playing, starting next track...");
            _playbackStatus = PlaybackStatus.Playing;
            _ = Task.Run(async () => await PlayNext());
        }
        finally
        {
            _audioStateSemaphore.Release();
            _audioQueueSemaphore.Release();
        }
    }
}

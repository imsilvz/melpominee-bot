using Azure.Identity;
using Azure.Storage.Blobs;
using Jitbit.Utils;
using Melpominee.Models;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Melpominee.Services;
public class MelpomineeAudioService
{
    private static readonly FastCache<ulong, SemaphoreSlim> _semStore = new();
    private static readonly ConcurrentDictionary<ulong, VoiceInstance> _instances = new();
    public MelpomineeAudioService()
    {}

    public async Task<bool> JoinChannel(GatewayClient client, Guild guild, ulong? channelId)
    {
        var semaphore = _semStore.GetOrAdd(guild.Id, _ => new SemaphoreSlim(1), TimeSpan.MaxValue);
        await semaphore.WaitAsync();
        try
        {
            if (channelId is null)
            {
                // null channelId means we disconnect
                Console.WriteLine("Disconnecting!");
                await client.UpdateVoiceStateAsync(new(guild.Id, null));
                return true;
            }
            else
            {
                // check if we are already connected to a channel
                if (_instances.TryGetValue(guild.Id, out var voiceInstance))
                {
                    Console.WriteLine("Switching Channels!");
                    await client.UpdateVoiceStateAsync(new(guild.Id, channelId));
                }
                else
                {
                    Console.WriteLine("First Time Join!");
                    var voiceClient = await client.JoinVoiceChannelAsync(
                        guild.Id,
                        (ulong)channelId,
                        new VoiceClientConfiguration
                        {
                            Logger = new ConsoleLogger(),
                        }
                    );
                    await voiceClient.StartAsync();

                    voiceInstance = new VoiceInstance(voiceClient);
                    _instances.AddOrUpdate(guild.Id, voiceInstance, (key, oldValue) => voiceInstance);
                }
            }
            return true;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public VoiceInstance? GetVoiceInstance(Guild guild)
    {
        if (!_instances.TryGetValue(guild.Id, out var voiceInstance))
        {
            return null;
        }
        return voiceInstance;
    }

    public async ValueTask VoiceStateUpdated(GatewayClient client, VoiceState state)
    {
        if (state.UserId != client.Id)
            return;

        var semaphore = _semStore.GetOrAdd(state.GuildId, _ => new SemaphoreSlim(1), TimeSpan.MaxValue);
        await semaphore.WaitAsync();
        try
        {
            if (state.ChannelId is null)
            {
                _semStore.Remove(state.GuildId);
                if (_instances.TryRemove(state.GuildId, out var instance))
                {
                    instance.Dispose();
                }
                return;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}

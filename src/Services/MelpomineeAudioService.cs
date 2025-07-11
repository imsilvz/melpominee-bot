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
public class MelpomineeAudioService : IHostedService
{
    private BlobServiceClient _serviceClient;
    private BlobContainerClient _containerClient;
    private static readonly FastCache<ulong, SemaphoreSlim> _semStore = new();
    private static readonly ConcurrentDictionary<ulong, VoiceInstance> _instances = new();
    public MelpomineeAudioService()
    {
        _serviceClient = new BlobServiceClient(
            new Uri(SecretStore.Instance.GetSecret("AZURE_STORAGE_ACCOUNT_URI")),
            new DefaultAzureCredential()
        );

        string containerName = SecretStore.Instance.GetSecret("AZURE_STORAGE_CONTAINER");
        _containerClient = _serviceClient.GetBlobContainerClient(containerName);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> JoinChannel(GatewayClient client, Guild guild, ulong? channelId)
    {
        var semaphore = _semStore.GetOrAdd(guild.Id, _ => new SemaphoreSlim(1), TimeSpan.MaxValue);
        await semaphore.WaitAsync();
        try
        {
            if (channelId is null)
            {
                Console.WriteLine("Disconnecting!");
                await client.UpdateVoiceStateAsync(new(guild.Id, null));
                return true;
            }
            else
            {
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

    public async Task StartPlayback(GatewayClient client, Guild guild)
    {
        if (!_instances.TryGetValue(guild.Id, out var voiceInstance))
        {
            return;
        }

    }

    public async Task QueueAudio(GatewayClient client, Guild guild, AudioSource source)
    {
        if (!_instances.TryGetValue(guild.Id, out var voiceInstance))
        {
            return;
        }

    }

    public async Task StopPlayback(GatewayClient client, Guild guild)
    {
        if (!_instances.TryGetValue(guild.Id, out var voiceInstance))
        {
            return;
        }

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

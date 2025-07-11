using Melpominee.Models;
using Melpominee.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Melpominee.Commands;
public class VoiceCommandModule(MelpomineeAudioService _audioService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("summon", "Summon Melpominee to a channel.", Contexts = [InteractionContextType.Guild])]
    public async Task JoinVoice()
    {
        var guild = Context.Guild!;

        // Get the user voice state
        if (!guild.VoiceStates.TryGetValue(Context.User.Id, out var voiceState))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "You must either be in a voice channel, or specify a voice channel to join!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }

        // Join the voice channel
        var client = Context.Client;
        await _audioService.JoinChannel(client, guild, voiceState.ChannelId.GetValueOrDefault());

        // Respond to the interaction
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Successfully joined the voice channel!",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    [SlashCommand("dismiss", "Dismiss Melpominee from all voice channels.", Contexts = [InteractionContextType.Guild])]
    public async Task LeaveVoice()
    {
        var client = Context.Client;
        var guild = Context.Guild!;

        if (!guild.VoiceStates.TryGetValue(client.Id, out var voiceState))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }

        // Leave the voice channel
        await _audioService.JoinChannel(client, guild, null);

        // Respond to the interaction
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Successfully disconnected from voice!",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    // Start queue playback. Optionally, specify a new source to play immediately.
    // If playback is ongoing, the current track will be skipped and the new source will start immediately.
    // If the queue is empty and no source is specified, an error message will be displayed.
    [SlashCommand("play", "Start audio playback in the current voice channel.", Contexts = [InteractionContextType.Guild])]
    public async Task Play()
    {
        AudioSource audioSource = new("/mnt/z/mp3/BGM_EX4_Event_05.mp3");
        VoiceInstance? voiceInstance = _audioService.GetVoiceInstance(Context.Guild!);
        if (voiceInstance is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        if (await voiceInstance.GetQueueLength() <= 0)
        {
            await voiceInstance.QueueAudio(audioSource, true);
        }
        await voiceInstance.StartPlayback();
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Ok.",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    // Pause queue playback. This should retain the current playback position which will be resumed when the /play command is called.
    [SlashCommand("pause", "Pause audio playback in the current voice channel.", Contexts = [InteractionContextType.Guild])]
    public async Task Pause()
    {
        VoiceInstance? voiceInstance = _audioService.GetVoiceInstance(Context.Guild!);
        if (voiceInstance is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        await voiceInstance.PausePlayback();
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Pausing playback. Resume with /play when ready!",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    // Pause queue playback. This should retain the current playback position which will be resumed when the /play command is called.
    [SlashCommand("stop", "Stop audio playback in the current voice channel.", Contexts = [InteractionContextType.Guild])]
    public async Task Stop()
    {
        VoiceInstance? voiceInstance = _audioService.GetVoiceInstance(Context.Guild!);
        if (voiceInstance is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        await voiceInstance.StopPlayback();
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Stopping playback.",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    // Add a new audio source to the queue.
    [SlashCommand("queue", "Queue audio for playback.", Contexts = [InteractionContextType.Guild])]
    public async Task Queue()
    {
        VoiceInstance? voiceInstance = _audioService.GetVoiceInstance(Context.Guild!);
        if (voiceInstance is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Ok.",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    // Skip the currently playing (or paused) audio track.
    [SlashCommand("skip", "Skip the currently playing audio track.", Contexts = [InteractionContextType.Guild])]
    public async Task Skip()
    {
        VoiceInstance? voiceInstance = _audioService.GetVoiceInstance(Context.Guild!);
        if (voiceInstance is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        await voiceInstance.SkipAudio();
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"Skipping current track.",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }

    [SlashCommand("clear", "Clear the audio queue.", Contexts = [InteractionContextType.Guild])]
    public async Task Clear()
    {
        VoiceInstance? voiceInstance = _audioService.GetVoiceInstance(Context.Guild!);
        if (voiceInstance is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Melpominee is not currently connected to any voice channels!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        await voiceInstance.ClearQueue();
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = $"The queue has been cleared.",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
    }
}

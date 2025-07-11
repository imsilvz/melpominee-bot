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

    [SlashCommand("play", "Play a song in the voice channel.", Contexts = [InteractionContextType.Guild])]
    public async Task Play()
    {
        AudioSource audioSource = new("test");
        await _audioService.QueueAudio(Context.Client, Context.Guild!, audioSource);
        await _audioService.StartPlayback(Context.Client, Context.Guild!);
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
}

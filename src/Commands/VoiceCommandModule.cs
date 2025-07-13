using Melpominee.Models;
using Melpominee.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Npgsql;
using System.Text.RegularExpressions;
using System.Data;

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
}

[SlashCommand("playback", "Audio Playback", Contexts = [InteractionContextType.Guild])]
public class VoiceAudioCommandModule(MelpomineeAudioService _audioService) : ApplicationCommandModule<ApplicationCommandContext>
{
    // Start queue playback. Optionally, specify a new source to play immediately.
    // If playback is ongoing, the current track will be skipped and the new source will start immediately.
    // If the queue is empty and no source is specified, an error message will be displayed.
    [SubSlashCommand("start", "Start audio playback in the current voice channel.")]
    public async Task Play([SlashCommandParameter(Name = "url", Description = "URL of song to add to queue")] string? @videoUrl = null)
    {
        var interaction = Context.Interaction;
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
        await interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        if (videoUrl is not null)
        {
            // Regex!
            var rgx1 = Regex.Match(videoUrl, @"youtube\.com\/watch\?v=(.*?)(?:&|$)");
            var rgx2 = Regex.Match(videoUrl, @"youtu\.be\/(.*?)(?:\?|$)");
            if (!(rgx1.Success || rgx2.Success))
            {
                await RespondAsync(
                    InteractionCallback.Message(
                        new()
                        {
                            Content = "Invalid playback URL!",
                            Flags = MessageFlags.Ephemeral
                        }
                    )
                );
                return;
            }
            string videoId = rgx1.Success ? rgx1.Groups[1].Value : rgx2.Groups[1].Value;

            AudioSource audioSource = new(videoId);
            if (!audioSource.GetCached())
                await audioSource.Precache();

            // immediately start playback by adding to front of queue and skipping current track
            await voiceInstance.QueueAudio(audioSource, true);
            if (await voiceInstance.GetPlaybackState() == VoiceInstance.PlaybackStatus.Playing)
                await voiceInstance.SkipAudio();
        }
        else
        {
            if (await voiceInstance.GetQueueLength() == 0 && await voiceInstance.GetPlaybackState() != VoiceInstance.PlaybackStatus.Paused)
            {
                await interaction.SendFollowupMessageAsync(
                    "The queue is empty! Please add a song to the queue before starting playback."
                );
                return;
            }
        }

        await voiceInstance.StartPlayback();
        await interaction.SendFollowupMessageAsync($"Ok.");
    }

    // Pause queue playback. This should retain the current playback position which will be resumed when the /play command is called.
    [SubSlashCommand("pause", "Pause audio playback in the current voice channel.")]
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
    [SubSlashCommand("stop", "Stop audio playback in the current voice channel.")]
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
    [SubSlashCommand("queue", "Queue audio for playback.")]
    public async Task Queue([SlashCommandParameter(Name = "url", Description = "URL of song to add to queue")] string @videoUrl)
    {
        var interaction = Context.Interaction;
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

        // Regex!
        var rgx1 = Regex.Match(videoUrl, @"youtube\.com\/watch\?v=(.*?)(?:&|$)");
        var rgx2 = Regex.Match(videoUrl, @"youtu\.be\/(.*?)(?:\?|$)");
        if (!(rgx1.Success || rgx2.Success))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Invalid playback URL!",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }
        string videoId = rgx1.Success ? rgx1.Groups[1].Value : rgx2.Groups[1].Value;
        await interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        AudioSource audioSource = new(videoId);
        if (!audioSource.GetCached())
            await audioSource.Precache();
        await voiceInstance.QueueAudio(audioSource);
        await interaction.SendFollowupMessageAsync($"Done.");
    }

    // Skip the currently playing (or paused) audio track.
    [SubSlashCommand("skip", "Skip the currently playing audio track.")]
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

    [SubSlashCommand("clear", "Clear the audio queue.")]
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

    [SubSlashCommand("playlist", "Manage available playlists.")]
    public class VoiceAudioPlaylistCommandModule(MelpomineeAudioService _audioService, DataContext _dataContext) : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SubSlashCommand("create", "Create a new playlist.")]
        public async Task PlaylistCreate([SlashCommandParameter(Name = "name", Description = "The name of the playlist you want to create.")] string playlistName)
        {
            try
            {
                // make connection and insert the new playlist row
                await using (var conn = await _dataContext.GetConnection())
                await using (var command = new NpgsqlCommand("INSERT INTO melpominee_audio.playlists (owner, playlist_name) VALUES ($1, $2)", conn)
                {
                    Parameters =
                {
                    new() { Value = (long)Context.User.Id, DbType = DbType.Int64 },
                    new() { Value = playlistName }
                }
                })
                await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException e)
            {
                // Unique Key Conflict
                if (e.SqlState == "23505")
                {
                    await RespondAsync(
                        InteractionCallback.Message(
                            new()
                            {
                                Content = $"You already have a playlist with this name.",
                                Flags = MessageFlags.Ephemeral
                            }
                        )
                    );
                    return;
                }
                throw;
            }

            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = $"Your \"{playlistName}\" playlist has been created.",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
        }

        [SubSlashCommand("delete", "Delete an existing playlist.")]
        public async Task PlaylistDelete(
            [SlashCommandParameter(
                Name = "name", 
                Description = "The name of the playlist you want to delete.", 
                AutocompleteProviderType = typeof(PlaylistNameAutocompleteProvider))] string stringId)
        {
            long playlistId;
            if(!long.TryParse(stringId, out playlistId))
            {
                // the autocomplete will submit the id itself. if nothing matches the id, it comes in as text.
                await RespondAsync(
                    InteractionCallback.Message(
                        new()
                        {
                            Content = $"The specified playlist was not found. Please try again.",
                            Flags = MessageFlags.Ephemeral
                        }
                    )
                );
                return;
            }

            // make connection and delete the playlist. we check owner to ensure someone doesnt attempt to delete someone elses playlist
            await using (var conn = await _dataContext.GetConnection())
            await using (var command = new NpgsqlCommand("DELETE FROM melpominee_audio.playlists WHERE id = $1 AND owner = $2", conn)
            {
                Parameters =
                {
                    new() { Value = playlistId, DbType = DbType.Int64 },
                    new() { Value = (long)Context.User.Id, DbType = DbType.Int64 }
                }
            })
            { 
                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected < 1)
                {
                    await RespondAsync(
                        InteractionCallback.Message(
                            new()
                            {
                                Content = $"The specified playlist was not found. Please try again.",
                                Flags = MessageFlags.Ephemeral
                            }
                        )
                    );
                    return;
                }
            }

            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = $"Your playlist was successfully deleted.",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
        }

        [SubSlashCommand("add", "Add a new track to an existing playlist.")]
        public async Task PlaylistAdd(
            [SlashCommandParameter(Name = "name", Description = "The name of the playlist you want to add to.")] string playlistName,
            [SlashCommandParameter(Name = "url", Description = "The url of the track you would like to add.")] string @videoUrl)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = $"Playlist Management Test",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
        }

        [SubSlashCommand("remove", "Remove a track from an existing playlist.")]
        public async Task PlaylistRemove(
            [SlashCommandParameter(Name = "name", Description = "The name of the playlist you want to remove from.")] string playlistName,
            [SlashCommandParameter(Name = "id", Description = "The ID of the track you would like to remove.")] string @videoUrl)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = $"Playlist Management Test",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
        }
    }
}

public class PlaylistNameAutocompleteProvider(DataContext _dataContext) : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var input = option.Value!;
        var playlists = new List<(long, string)>();

        await using (var conn = await _dataContext.GetConnection())
        await using (var command = new NpgsqlCommand("SELECT id, playlist_name FROM melpominee_audio.playlists WHERE owner = $1", conn)
        {
            Parameters =
                {
                    new() { Value = (long)context.User.Id, DbType = DbType.Int64 }
                }
        })
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                playlists.Add((reader.GetInt64(0), reader.GetString(1)));
            }
        }

        // filter by input
        if (input != "")
        {
            playlists = playlists.Where(kvp => kvp.Item2.ToLower().Contains(input.ToLower())).ToList();
        }

        var result = playlists.Select
        (
            kvp => new ApplicationCommandOptionChoiceProperties(kvp.Item2, kvp.Item1.ToString())
        );
        return result;
    }
}

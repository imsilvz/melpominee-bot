using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
namespace Melpominee.Interactions
{
    public class PlaylistAutocomplete : MelpomineeInteraction
    {
        public PlaylistAutocomplete(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Id => "play-playlist";
        public override async Task Execute(DiscordSocketClient client, SocketInteraction interaction)
        {
            var autocomplete = (SocketAutocompleteInteraction)interaction;
            var autocompleteText = (string)autocomplete.Data.Current.Value;
            var autocompleteResults = _audioService
                .GetPlaylists()
                .Where((playlistName) => playlistName.ToLower().Contains(autocompleteText.ToLower()))
                .Select((playlistName) => new AutocompleteResult(playlistName, playlistName))
                .Take(25)
                .ToList();
            await autocomplete.RespondAsync(autocompleteResults);
        }
    }
}

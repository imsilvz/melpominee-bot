using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Melpominee.Models;
using Microsoft.Extensions.Hosting;

using File = System.IO.File;
using static Melpominee.Models.AudioSource;
using System.Text.RegularExpressions;
namespace Melpominee.Services
{
    public class AudioService : IHostedService
    {
        private BlobServiceClient _serviceClient;
        private BlobContainerClient _containerClient;
        public Dictionary<string, List<string>> _playlistDict; // contains file mapping for playlists
        private Dictionary<string, string> _playlistIdDict; // contains id mapping for playlists
        private ConcurrentDictionary<ulong, AudioConnection> _connections;
        public AudioService()  
        {
            _serviceClient = new BlobServiceClient(
                new Uri(SecretStore.Instance.GetSecret("AZURE_STORAGE_ACCOUNT_URI")),
                new DefaultAzureCredential()
            );

            string containerName = SecretStore.Instance.GetSecret("AZURE_STORAGE_CONTAINER");
            _containerClient = _serviceClient.GetBlobContainerClient(containerName);
            _playlistDict = new Dictionary<string, List<string>>();
            _playlistIdDict = new Dictionary<string, string>();
            _connections = new ConcurrentDictionary<ulong, AudioConnection>();
        }

        public async Task<string> CreatePlaylist(string name, bool upload = true)
        {
            PlaylistData metadata = new PlaylistData
            {
                Id = Guid.NewGuid().ToString(),
                PlaylistName = name,
            };

            Console.WriteLine($"Generating metadata for new playlist \'{name}\'!");
            Directory.CreateDirectory(Path.Combine("assets", metadata.Id));
            string blobPath = Path.Combine("playlists", metadata.Id, "meta.txt");
            string metadataPath = Path.Combine("assets", blobPath);
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));

            if (upload)
            {
                Console.WriteLine($"Beginning upload of metadata for new playlist \'{name}\'!");
                var blobClient = _containerClient.GetBlobClient(blobPath);
                await blobClient.UploadAsync(metadataPath, true);
            }

            _playlistIdDict[name] = metadata.Id;
            return metadata.Id;
        }

        public async Task UploadPlaylist(string playlistId)
        {
            Console.WriteLine($"Beginning upload of playlist ({playlistId})!");
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var sourceFiles = Directory.GetFiles(Path.Combine(executingDirectory, "assets", playlistId));
            foreach(var sourceFile in sourceFiles)
            {
                var sourceFileName = Path.GetFileName(sourceFile);
                string blobPath = Path.Combine("playlists", playlistId, sourceFileName);
                var blobClient = _containerClient.GetBlobClient(blobPath);
                await blobClient.UploadAsync(sourceFile, true);
            }
        }

        public async Task ReloadPlaylists()
        {
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var playlistDirectories = Directory.GetDirectories(Path.Combine(executingDirectory, "assets"));
            foreach (var playlistPath in playlistDirectories)
            {
                var playlistId = Path.GetFileName(playlistPath);
                // initialize file list
                _playlistDict[playlistId] = new List<string>();
                // iterate and setup
                var playlistFiles = Directory.GetFiles(playlistPath);
                foreach (var playlistFile in playlistFiles)
                {
                    var fileName = Path.GetFileName(playlistFile);
                    if (fileName == "meta.txt")
                    {
                        // load directory metadata
                        using (var reader = File.OpenText(Path.Combine(playlistPath, "meta.txt")))
                        {
                            var metaText = await reader.ReadToEndAsync();
                            var metaModel = JsonSerializer.Deserialize<PlaylistData>(metaText);
                            if (metaModel is not null)
                            {
                                _playlistIdDict[metaModel.PlaylistName] = playlistId;
                                Console.WriteLine($"Found Playlist \'{metaModel.PlaylistName}\' ({playlistId})");
                            }
                        }
                    }
                    else if (Path.GetFileNameWithoutExtension(playlistFile) == "thumb") { }
                    else
                    {
                        _playlistDict[playlistId].Add(playlistFile);
                    }
                }
            }

            var r = new Random();
            foreach (var playlistId in _playlistDict.Keys)
            {
                var shuffledPlaylist = _playlistDict[playlistId].OrderBy(x => r.Next()).ToList();
                _playlistDict[playlistId] = shuffledPlaylist;
            }
        }

        private async Task Initialize()
        {
            // Synchronize Playlists
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync(prefix: "playlists/"))
            {
                string blobName = blobItem.Name;
                var regex = new Regex(Regex.Escape("playlists/"));
                string playlistFilePath = regex.Replace(blobName, "assets/", 1);
                string? playlistPath = Path.GetDirectoryName(playlistFilePath);
                // skip if already exists
                if (!File.Exists(playlistFilePath))
                {
                    // create directory if not exists
                    if (playlistPath is not null && !Directory.Exists(playlistPath))
                        Directory.CreateDirectory(playlistPath);
                    // download files to sync assets with bucket
                    var blobClient = _containerClient.GetBlobClient(blobName);
                    await blobClient.DownloadToAsync(playlistFilePath);
                }
            }
            await ReloadPlaylists();
        }

        public List<string> GetPlaylists()
        {
            return _playlistIdDict.Keys.ToList();
        }

        public string? GetPlaylistId(string playlistName)
        {
            if(_playlistIdDict.TryGetValue(playlistName, out var playlistId))
                return playlistId;
            return null;
        }

        public async Task<AudioConnection> Connect(IVoiceChannel channel, bool deaf=true, bool mute=false)
        {
            var audioConn = new AudioConnection(channel);
            await audioConn.Connect(deaf, mute);
            _connections.AddOrUpdate(channel.GuildId, audioConn, (key, oldVal) =>
            {
                oldVal.Disconnect().ConfigureAwait(false);
                return audioConn;
            });
            return audioConn;
        }
        
        public async Task<bool> Disconnect(IGuild guild)
        {
            if (!_connections.TryRemove(guild.Id, out var connection))
                return false;
            await connection.Disconnect();
            return true;
        }

        public async Task<bool> SkipPlayback(SocketGuild guild)
        {
            if (!_connections.TryGetValue(guild.Id, out var audioConn))
                return false;
            await audioConn.StopPlayback(false);
            return true;
        }

        public async Task<bool> StopPlayback(SocketGuild guild, bool waitForIdle = false)
        {
            if (!_connections.TryGetValue(guild.Id, out var audioConn))
                    return false;
            audioConn.ClearAudioQueue();
            await audioConn.StopPlayback(waitForIdle);
            return true;
        }

        public async Task PlayAudio(SocketGuild guild, AudioSource audioSource)
        {
            if (!_connections.TryGetValue(guild.Id, out var audioConn))
            {
                var currChannel = guild.CurrentUser.VoiceChannel;
                if (guild.CurrentUser.VoiceChannel != null)
                {
                    audioConn = await Connect(currChannel);
                }
                else
                {
                    return;
                }
            }
            await audioConn.QueueSource(audioSource, true);
            await audioConn.StartPlayback();
        }

        public async Task<bool> PlayPlaylist(SocketGuild guild, string playlistName)
        {
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            string? playlistId;
            if (!_playlistIdDict.TryGetValue(playlistName, out playlistId))
                return false;

            if (!_connections.TryGetValue(guild.Id, out var audioConn))
                return false;

            var playlistFileList = _playlistDict[playlistId];
            if (playlistFileList is null || playlistFileList.Count == 0)
                return false;

            foreach(var audioFileName in playlistFileList) 
            {
                var audioFilePath = Path.Combine(
                    executingDirectory, 
                    "assets", 
                    playlistId, 
                    audioFileName
                );
                var audioSource = new AudioSource(SourceType.Local, audioFilePath);
                await audioConn.QueueSource(audioSource, false);
            }
            await audioConn.StartPlayback();
            return true;
        }

        public async Task QueueAudio(SocketGuild guild, AudioSource audioSource)
        {
            if (!_connections.TryGetValue(guild.Id, out var audioConn))
                return;
            await audioConn.QueueSource(audioSource, false);
            await audioConn.StartPlayback();
        }

        public bool SetQueueLoop(SocketGuild guild, bool toggle)
        {
            if (!_connections.TryGetValue(guild.Id, out var audioConn))
                return false;
            audioConn.LoopQueue = toggle;
            return true;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Initialize();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

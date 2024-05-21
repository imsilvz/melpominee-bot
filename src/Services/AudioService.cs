using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Melpominee.Models;
using Melpominee.Utility;
using Microsoft.Extensions.Hosting;

using File = System.IO.File;
using static Melpominee.Models.AudioSource;
namespace Melpominee.Services
{
    public class AudioService : IHostedService
    {
        public enum PlaybackStatus
        {
            Unknown = 0,
            Idle    = 1,
            Playing = 2,
            Error   = 3,
        }

        private AudioPlayer _player;
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

            _player = new AudioPlayer();
            _player.PlaybackFinished += QueueHandler;
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
            string blobPath = Path.Combine(metadata.Id, "meta.txt");
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
                string blobPath = Path.Combine(playlistId, sourceFileName);
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
            //await ConvertLegacyAssets();
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                string blobName = blobItem.Name;
                string playlistFilePath = Path.Combine("assets", blobName);
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

        private async Task ConvertLegacyAssets()
        {
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var legacyDirectories = Directory.GetDirectories(Path.Combine(executingDirectory, "legacy"));
            foreach (var legacyPath in legacyDirectories)
            {
                var playlistName = Path.GetFileName(legacyPath);
                var playlistId = await CreatePlaylist(playlistName, false);
                foreach (var legacyFile in Directory.GetFiles(legacyPath))
                {
                    var legacyFileName = Path.GetFileName(legacyFile);
                    var legacyFileNameNoExt = Path.GetFileNameWithoutExtension(legacyFile);
                    if (legacyFileNameNoExt == "thumb")
                    {
                        File.Copy(legacyFile, Path.Combine(executingDirectory, "assets", playlistId, legacyFileName));
                    }
                }
                foreach (var legacyFile in Directory.GetFiles(Path.Combine(legacyPath, "audio")))
                {
                    var legacyFileName = Path.GetFileName(legacyFile);
                    Console.WriteLine(legacyFileName);
                    File.Copy(legacyFile, Path.Combine(executingDirectory, "assets", playlistId, legacyFileName));
                }
                await UploadPlaylist(playlistId);
            }
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

        public async Task<bool> Connect(IVoiceChannel channel, bool deaf=true, bool mute=false)
        {
            var audioClient = await channel.ConnectAsync(deaf, mute, false);
            var connectionModel = new AudioConnection
            { 
                Client = audioClient,
                Channel = channel,
                Guild = channel.Guild,
                AudioQueue = new ConcurrentQueue<AudioSource>(),
                PlaybackStatus = PlaybackStatus.Idle,
                PlaybackCancellationToken = new CancellationTokenSource(),
            };
            _connections.AddOrUpdate(channel.GuildId, connectionModel, (key, oldVal) =>
            {
                oldVal.Client.Dispose();
                return connectionModel;
            });
            audioClient.Connected += connectionModel.OnConnected;
            audioClient.Disconnected += connectionModel.OnDisconnected;
            return true;
        }
        
        public async Task<bool> Disconnect(IGuild guild)
        {
            if (!_connections.TryRemove(guild.Id, out var connection))
                return false;
            await connection.Client.StopAsync();
            connection.Client.Dispose();
            return true;
        }

        public async Task<bool> StartPlaylist(SocketGuild guild, string playlistName)
        {
            // fetch playlist id
            string? playlistId;
            if (!_playlistIdDict.TryGetValue(playlistName, out playlistId))
                return false;
            // queue next item
            _ = Task.Run(async () =>
            {
                await StopPlayback(guild);
                await PlayPlaylist(guild, playlistId);
            });
            return true;
        }

        public async Task<bool> StopPlayback(SocketGuild guild)
        {
            if (!_connections.TryGetValue(guild.Id, out var connectionData))
                return false;
            connectionData.PlaybackCancellationToken.Cancel();
            while(connectionData is not null) 
            {
                if (connectionData.PlaybackStatus != PlaybackStatus.Playing) break;
                await Task.Delay(1);
            }
            return true;
        }

        public async Task PlayAudio(SocketGuild guild, AudioSource audioSource)
        {
            if (!_connections.TryGetValue(guild.Id, out var connectionData))
                return;
            var audioClient = connectionData.Client;

            // Setup cancellation token to stop if needs be
            var cancellationTokenSource = connectionData.PlaybackCancellationToken;
            if (cancellationTokenSource.IsCancellationRequested || !cancellationTokenSource.TryReset())
            {
                cancellationTokenSource = new CancellationTokenSource();
                connectionData.PlaybackCancellationToken = cancellationTokenSource;
            }
            var cancellationToken = cancellationTokenSource.Token;

            try
            {
                Console.WriteLine($"[{guild.Id}] Beginning playback for {audioSource.GetSource()}");
                await _player.StartPlayback(connectionData, audioSource, cancellationToken);
            }
            catch (OperationCanceledException)
            { }
            finally
            { 
                audioSource.Dispose();
            }
        }

        private async Task PlayPlaylist(SocketGuild guild, string playlistId, int playlistIndex = 0)
        {
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            if (!_connections.TryGetValue(guild.Id, out var connectionData))
                return;
            var audioClient = connectionData.Client;

            var cancellationTokenSource = connectionData.PlaybackCancellationToken;
            if (cancellationTokenSource.IsCancellationRequested || !cancellationTokenSource.TryReset())
            {
                cancellationTokenSource = new CancellationTokenSource();
                connectionData.PlaybackCancellationToken = cancellationTokenSource;
            }
            var cancellationToken = cancellationTokenSource.Token;

            // generate path
            var playlistFileList = _playlistDict[playlistId];
            if (playlistFileList is null || playlistFileList.Count == 0)
                return;
            if (!(playlistFileList.Count > playlistIndex))
                playlistIndex = 0;
            var audioFileName = _playlistDict[playlistId][playlistIndex];
            var audioPath = Path.Combine(executingDirectory, "assets", playlistId, audioFileName);

            // spin up process
            using (var ffmpeg = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{audioPath}\" -f s16le -ac 2 -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            }))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = audioClient.CreatePCMStream(AudioApplication.Music, bitrate: 128 * 1024, bufferMillis: 250, packetLoss: 40))
            {
                int audioBufferSize = 1024;
                byte[] audioBuffer = new byte[audioBufferSize];
                bool shouldExit = false;
                bool cancelled = false;
                try
                {
                    connectionData.PlaybackStatus = PlaybackStatus.Playing;
                    while (
                        audioClient.ConnectionState == ConnectionState.Connected &&
                        !shouldExit
                    )
                    {
                        int bytesRead = await output.ReadAsync(audioBuffer, 0, audioBufferSize, cancellationToken);

                        // if no more data, exit
                        if (bytesRead <= 0)
                        {
                            shouldExit = true;
                            break;
                        }

                        await discord.WriteAsync(audioBuffer, 0, bytesRead, cancellationToken);
                    }
                }
                catch (OperationCanceledException) { cancelled = true; }
                catch { connectionData.PlaybackStatus = PlaybackStatus.Error; }
                finally
                {
                    await discord.FlushAsync();
                    if (cancelled)
                        connectionData.PlaybackStatus = PlaybackStatus.Idle;
                    else if (connectionData.PlaybackStatus == PlaybackStatus.Playing)
                    {
                        // continue to next track!
                        _ = Task.Run(async () =>
                        {
                            await PlayPlaylist(guild, playlistId, playlistIndex + 1);
                        });
                    }
                }
            }
        }

        public void ClearAudioQueue(SocketGuild guild)
        {
            if (!_connections.TryGetValue(guild.Id, out var connectionData))
                return;
            connectionData.AudioQueue.Clear();
        }

        public async Task QueueAudio(SocketGuild guild, AudioSource audioSource)
        {
            if (!_connections.TryGetValue(guild.Id, out var connectionData))
                return;

            // e
            Task cacheTask;
            if (!audioSource.GetCached())
                cacheTask = audioSource.Precache();
            else
                cacheTask = Task.CompletedTask;

            if (connectionData.PlaybackStatus != PlaybackStatus.Playing)
            {
                await PlayAudio(guild, audioSource);
            }
            else
            {
                connectionData.AudioQueue.Enqueue(audioSource);
            }
            await cacheTask;
        }

        private void QueueHandler(object? sender, AudioConnection audioConnection)
        {
            var guild = audioConnection.Guild;
            if (audioConnection.AudioQueue.TryDequeue(out var audioSource))
            {
                _ = Task.Run(async () =>
                {
                    await PlayAudio((SocketGuild)guild, audioSource);
                });
            }
            else
            {
                audioConnection.PlaybackStatus = PlaybackStatus.Idle;
            }
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

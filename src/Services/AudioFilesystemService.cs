using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Melpominee.Models;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;

namespace Melpominee.Services
{
    public class AudioFilesystemService : IHostedService
    {
        private BlobServiceClient _serviceClient;
        private BlobContainerClient _containerClient;
        private ConcurrentDictionary<string, string> _playlistCache;
        public AudioFilesystemService()  
        {
            _serviceClient = new BlobServiceClient(
                new Uri(SecretStore.Instance.GetSecret("AZURE_STORAGE_ACCOUNT_URI")),
                new DefaultAzureCredential()
            );

            string containerName = SecretStore.Instance.GetSecret("AZURE_STORAGE_CONTAINER");
            _containerClient = _serviceClient.GetBlobContainerClient(containerName);
            _playlistCache = new ConcurrentDictionary<string, string>();
        }

        public async Task CreatePlaylist(string name)
        {
            MelpomineePlaylistData metadata = new MelpomineePlaylistData
            {
                Id = Guid.NewGuid().ToString(),
                PlaylistName = name,
            };

            Console.WriteLine($"Generating metadata for new playlist \'{name}\'!");
            Directory.CreateDirectory(Path.Combine("assets", metadata.Id));
            string blobPath = Path.Combine(metadata.Id, "meta.txt");
            string metadataPath = Path.Combine("assets", blobPath);
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata));

            Console.WriteLine($"Beginning upload of metadata for new playlist \'{name}\'!");
            var blobClient = _containerClient.GetBlobClient(blobPath);
            await blobClient.UploadAsync(metadataPath, true);
        }

        private async Task Initialize()
        {
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                string blobName = blobItem.Name;
                string metadataFilePath = Path.Combine("assets", blobName);
                string? metadataPath = Path.GetDirectoryName(metadataFilePath);
                // skip if already exists
                if (!File.Exists(metadataFilePath))
                {
                    // create directory if not exists
                    if (metadataPath is not null && !Directory.Exists(metadataPath))
                        Directory.CreateDirectory(metadataPath);
                    // download files to sync assets with bucket
                    var blobClient = _containerClient.GetBlobClient(blobName);
                    await blobClient.DownloadToAsync(metadataFilePath);
                }

                // if its a metadata file, load playlist data
                if (Path.GetFileName(metadataFilePath) == "meta.txt")
                {
                    using (var reader = File.OpenText(metadataFilePath))
                    {
                        var metaText = await reader.ReadToEndAsync();
                        var metaModel = JsonSerializer.Deserialize<MelpomineePlaylistData>(metaText);
                        if (metaModel is not null)
                        {
                            _playlistCache.AddOrUpdate(metaModel.PlaylistName, metaModel.Id, (newVal, oldVal) => newVal);
                            Console.WriteLine($"Found Playlist \'{metaModel.PlaylistName}\'");
                        }
                    }
                }
            }
        }

        public List<string> GetPlaylists()
        {
            Console.WriteLine(_playlistCache.Keys.Count);
            return _playlistCache.Keys.ToList();
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

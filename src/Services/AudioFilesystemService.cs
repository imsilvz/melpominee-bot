using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Melpominee.Models;
using System.Text.Json;
namespace Melpominee.Services
{
    public class AudioFilesystemService
    {
        private BlobServiceClient _serviceClient;
        private BlobContainerClient _containerClient;
        private Dictionary<string, string> _playlistCache;
        public AudioFilesystemService() 
        {
            _serviceClient = new BlobServiceClient(
                new Uri(SecretStore.Instance.GetSecret("AZURE_STORAGE_ACCOUNT_URI")),
                new DefaultAzureCredential()
            );

            string containerName = SecretStore.Instance.GetSecret("AZURE_STORAGE_CONTAINER");
            _containerClient = _serviceClient.GetBlobContainerClient(containerName);
            _playlistCache = new Dictionary<string, string>();
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

        public async Task Init()
        {
            Console.WriteLine("Begin");
            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
            {
                string blobName = blobItem.Name;
                string metadataFilePath = Path.Combine("assets", blobName);
                string? metadataPath = Path.GetDirectoryName(metadataFilePath);
                if (metadataPath is not null && !Directory.Exists(metadataPath))
                    Directory.CreateDirectory(metadataPath);
                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.DownloadToAsync(metadataFilePath);
                Console.WriteLine("\t" + blobName);
            }
            Console.WriteLine("Complete");
        }
    }
}

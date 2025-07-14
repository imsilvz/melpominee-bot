using Azure.Identity;
using Azure.Storage.Blobs;
using Melpominee.Services;
using System.Diagnostics;
using System.Reflection;

namespace Melpominee.Models;
public class AudioSource
{
    private string _sourceId;
    private bool _caching = false;
    public AudioSource(string sourceId)
    {
        _sourceId = sourceId;
    }

    public async Task<bool> Precache(string? playlistId = null)
    {
        // mark caching
        _caching = true;

        if (GetCached())
        {
            _caching = false;
            return true;
        }

        var proxyAddr = SecretStore.Instance.GetSecret("MELPOMINEE_PROXY");
        var proxyFull = "";
        if (!string.IsNullOrEmpty(proxyAddr))
        {
            proxyFull = $"--proxy \"socks5://{proxyAddr}/\"";
        }

        Console.WriteLine($"Beginning caching for {_sourceId}");
        var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var rootCachePath = Path.Combine(executingDirectory, "cache", "yt");
        var cachePath = Path.Combine(rootCachePath, $"{_sourceId}.m4a");
        Directory.CreateDirectory(rootCachePath);

        if (File.Exists(cachePath))
        {
            File.Delete(cachePath);
        }

        // check if exists in blob storage
        string storageName = SecretStore.Instance.GetSecret("AZURE_STORAGE_ACCOUNT_URI");
        string containerName = SecretStore.Instance.GetSecret("AZURE_STORAGE_CONTAINER");
        var storageClient = new BlobServiceClient(
            new Uri(storageName),
            new DefaultAzureCredential()
        );
        var containerClient = storageClient.GetBlobContainerClient(containerName);
        string blobPath = Path.Combine("cache", "yt", $"{_sourceId}.m4a");
        var blobClient = containerClient.GetBlobClient(blobPath);
        if (await blobClient.ExistsAsync())
        {
            // fetch from blob storage
            Console.WriteLine($"Fetching from blob storage ({_sourceId}.m4a)");
            await blobClient.DownloadToAsync(cachePath);
        }
        else
        {
            // run yt-dlp process
            var processInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"https://www.youtube.com/watch?v={_sourceId} {proxyFull} -v -x --audio-format m4a --audio-quality 0 -o {cachePath}",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                CreateNoWindow = true
            };
            var process = Process.Start(processInfo);
            if (process is null)
            {
                _caching = false;
                Console.WriteLine($"Caching operation failed for {_sourceId}");
                return false;
            }

            // handle result
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
                _caching = false;
                Console.WriteLine($"Caching operation failed for {_sourceId}");
                return false;
            }
            else
            {
                // handle upload in separate thread
                _ = Task.Run(async () =>
                {
                    Console.WriteLine($"Beginning upload of cached item ({_sourceId}.m4a)");
                    await blobClient.UploadAsync(cachePath, true);
                    Console.WriteLine($"Upload complete ({_sourceId}.m4a)");
                });
            }
        }
        _caching = false;

        Console.WriteLine($"Caching operation completed for {_sourceId}");
        return true;
    }

    public bool IsCaching()
    {
        return _caching;
    }

    public bool GetCached()
    {
        var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var rootCachePath = Path.Combine(executingDirectory, "cache", "yt");
        var cachePath = Path.Combine(rootCachePath, $"{_sourceId}.m4a");
        return File.Exists(cachePath);
    }

    public Stream GetStream()
    {
        var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var rootCachePath = Path.Combine(executingDirectory, "cache", "yt");
        var cachePath = Path.Combine(rootCachePath, $"{_sourceId}.m4a");

        if (!GetCached())
        {
            throw new Exception($"Audio source {_sourceId} is not cached!");
        }

        Process? streamProcess = null;
        var processInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        var arguments = processInfo.ArgumentList;

        // Specify the input
        arguments.Add("-i");
        arguments.Add(cachePath);

        // Set the logging level to quiet mode
        arguments.Add("-loglevel");
        arguments.Add("-8");

        // Set the number of audio channels to 2 (stereo)
        arguments.Add("-ac");
        arguments.Add("2");

        // Set the output format to 16-bit signed little-endian
        arguments.Add("-f");
        arguments.Add("s16le");

        // Set the audio sampling rate to 48 kHz
        arguments.Add("-ar");
        arguments.Add("48000");

        // Direct the output to stdout
        arguments.Add("pipe:1");

        try
        {
            var process = Process.Start(processInfo);
            streamProcess = process;
        }
        catch
        {
            Console.WriteLine($"An error occurred while starting a file stream for {_sourceId}");
        }

        if (streamProcess is null) { throw new Exception("Something went wrong!"); }
        return streamProcess.StandardOutput.BaseStream;
    }
}

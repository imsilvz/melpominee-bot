﻿using Azure.Identity;
using Azure.Storage.Blobs;
using Melpominee.Services;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Melpominee.Models
{
    public class AudioSource : IDisposable
    {
        public enum SourceType
        {
            Unknown = 0,
            Local = 1,
            Networked = 2
        }

        // metadata
        private bool _caching = false;
        private SourceType _sourceType;
        private string _sourcePath;
        private Process? _streamProcess = null;
        public AudioSource(SourceType sourceType, string sourcePath) 
        { 
            _sourceType = sourceType;
            _sourcePath = sourcePath;
        }

        public async Task<bool> Precache(string? playlistId = null)
        {
            // mark caching
            _caching = true;

            var proxyAddr = SecretStore.Instance.GetSecret("MELPOMINEE_PROXY");
            var proxyFull = "";
            if (!string.IsNullOrEmpty(proxyAddr))
            {
                proxyFull = $"--proxy \"socks5://{proxyAddr}/\"";
            }

            Console.WriteLine($"Beginning caching for {_sourcePath}");
            if (_sourceType == SourceType.Networked)
            {
                var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var rootCachePath = Path.Combine(executingDirectory, "cache", "yt");
                var cachePath = Path.Combine(rootCachePath, $"{_sourcePath}.m4a");
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
                string blobPath = Path.Combine("cache", "yt", $"{_sourcePath}.m4a");
                var blobClient = containerClient.GetBlobClient(blobPath);
                if (await blobClient.ExistsAsync())
                {
                    // fetch from blob storage
                    Console.WriteLine($"Fetching from blob storage ({_sourcePath}.m4a)");
                    await blobClient.DownloadToAsync(cachePath);
                }
                else
                {
                    // run yt-dlp process
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "yt-dlp",
                        Arguments = $"https://www.youtube.com/watch?v={_sourcePath} {proxyFull} -v -x --audio-format m4a --audio-quality 0 -o {cachePath}",
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true
                    };
                    var process = Process.Start(processInfo);
                    if (process is null)
                    {
                        _caching = false;
                        Console.WriteLine($"Caching operation failed for {_sourcePath}");
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
                        Console.WriteLine($"Caching operation failed for {_sourcePath}");
                        return false;
                    }
                    else
                    {
                        // handle upload in separate thread
                        _ = Task.Run(async () => 
                        {
                            Console.WriteLine($"Beginning upload of cached item ({_sourcePath}.m4a)");
                            await blobClient.UploadAsync(cachePath, true);
                            Console.WriteLine($"Upload complete ({_sourcePath}.m4a)");
                        });
                    }
                }
            }
            else if(_sourceType == SourceType.Local)
            {
                if (string.IsNullOrEmpty(playlistId))
                    return false;
                _caching = true;

                // calculate path
                var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                var playlistPath = Path.Combine(executingDirectory, "assets", playlistId);
                var filePath = $"{playlistPath}/%(title)s.%(ext)s";
                var thumbPath = $"{playlistPath}/thumb.%(ext)s";

                // run yt-dlp process
                var processInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"https://www.youtube.com/watch?v={_sourcePath} {proxyFull} -v -x --audio-format m4a --audio-quality 0 -o {filePath} --write-thumbnail --convert-thumbnails png -o thumbnail:{thumbPath}",
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(processInfo);
                if (process is null)
                {
                    _caching = false;
                    Console.WriteLine($"Caching operation failed for {_sourcePath}");
                    return false;
                }

                // handle result
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    _caching = false;
                    Console.WriteLine($"Caching operation failed for {_sourcePath}");
                    return false;
                }
            }
            _caching = false;
            Console.WriteLine($"Caching operation completed for {_sourcePath}");
            return true;
        }

        public bool IsCaching()
        {
            return _caching;
        }
         
        public bool GetCached()
        {
            if (_sourceType == SourceType.Local) { return true; }
            var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var rootCachePath = Path.Combine(executingDirectory, "cache", "yt");
            var cachePath = Path.Combine(rootCachePath, $"{_sourcePath}.m4a");
            return File.Exists(cachePath);
        }

        public string GetSource()
        {
            return _sourcePath;
        }

        public SourceType GetSourceType()
        {
            return _sourceType;
        }

        public Stream GetStream()
        {
            if (_streamProcess is null)
            {
                if (_sourceType == SourceType.Local)
                {
                    _streamProcess = GetFileProcess(_sourcePath);
                }
                else if (_sourceType == SourceType.Networked)
                {
                    if (GetCached() && !_caching)
                    {
                        var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                        var rootCachePath = Path.Combine(executingDirectory, "cache", "yt");
                        var cachePath = Path.Combine(rootCachePath, $"{_sourcePath}.m4a");
                        _streamProcess = GetFileProcess(cachePath); 
                        Console.WriteLine($"Fetching from cache path ({_sourcePath}.m4a)");
                    }
                    else
                    {
                        _streamProcess = GetNetworkProcess(_sourcePath);
                    }
                }
            }
            if (_streamProcess is null) { throw new Exception("Something went wrong!"); }
            return _streamProcess.StandardOutput.BaseStream;
        }

        // yt-dlp -o - "https://www.youtube.com/watch?v=BYjt5E580PY" | ffmpeg -i pipe:0 -f s16le -ac 2 -ar 48000 pipe:1 | ffmpeg -f s16le -ac 2 -ar 48000 -i pipe:0 -c:a aac "C:\Users\rjyaw\Desktop\test.m4a"
        private Process? GetFileProcess(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-loglevel panic -i \"{path}\" -f s16le -ac 2 -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            try
            {
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    return process;
                }
            }
            catch
            {
                Console.WriteLine($"An error occurred while starting a file stream for {path}");
            }
            return null;
        }

        private Process? GetNetworkProcess(string videoId)
        {
            var proxyAddr = SecretStore.Instance.GetSecret("MELPOMINEE_PROXY");
            var proxyFull = "";
            if (!string.IsNullOrEmpty(proxyAddr))
            {
                proxyFull = $"--proxy \"socks5://{proxyAddr}/\"";
            }

            var processFileName = "cmd.exe";
            var processFileArgs = $"/C yt-dlp {proxyFull} -q -o - \"https://www.youtube.com/watch?v={videoId}\" | ffmpeg -loglevel panic -i pipe:0 -f s16le -ac 2 -ar 48000 pipe:1";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                processFileName = "/bin/sh";
                processFileArgs = $"-c \"yt-dlp {proxyFull} -q -o - https://www.youtube.com/watch?v={videoId} | ffmpeg -loglevel panic -i pipe:0 -f s16le -ac 2 -ar 48000 pipe:1\"";
            }
            var processInfo = new ProcessStartInfo
            {
                FileName = processFileName,
                Arguments = processFileArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            try
            {
                var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.EnableRaisingEvents = true;
                    process.ErrorDataReceived += (sender, e) => { Console.WriteLine("ERROR!"); };
                    return process;
                }
            }
            catch
            {
                Console.WriteLine($"An error occurred while starting a network stream for {videoId}");
            }
            return null;
        }

        public void Dispose()
        {
            if (_streamProcess is not null && !_streamProcess.HasExited)
            {
                _streamProcess.Kill();
            }
            _streamProcess = null;
            Console.WriteLine($"Audio Process has been stopped for {_sourcePath}");
        }
    }
}

using Azure.Storage.Files.Shares.Models;
using System;
using System.Diagnostics;

namespace Melpominee.Models;
public class AudioSource
{
    private string _sourcePath;
    public AudioSource(string path)
    {
        _sourcePath = path;
    }

    public Stream GetStream()
    {
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
        arguments.Add(_sourcePath);

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
            Console.WriteLine($"An error occurred while starting a file stream for {_sourcePath}");
        }

        if (streamProcess is null) { throw new Exception("Something went wrong!"); }
        return streamProcess.StandardOutput.BaseStream;
    }
}

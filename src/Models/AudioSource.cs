using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        ProcessStartInfo startInfo = new("ffmpeg")
        {
            RedirectStandardOutput = true,
        };
        var arguments = startInfo.ArgumentList;

        // Set reconnect attempts in case of a lost connection to 1
        arguments.Add("-reconnect");
        arguments.Add("1");

        // Set reconnect attempts in case of a lost connection for streamed media to 1
        arguments.Add("-reconnect_streamed");
        arguments.Add("1");

        // Set the maximum delay between reconnection attempts to 5 seconds
        arguments.Add("-reconnect_delay_max");
        arguments.Add("5");

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

        // Start the FFmpeg process
        var ffmpeg = Process.Start(startInfo)!;
        return ffmpeg.StandardOutput.BaseStream;
    }
}

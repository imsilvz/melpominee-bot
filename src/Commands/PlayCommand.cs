using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Melpominee.Interfaces;
using System.Diagnostics;
using System.IO;
namespace Melpominee.Commands
{
    public class PlayCommand : ISlashCommand
    {

        public string Name => "play";

        public string Description => "Begin streaming a playlist.";

        public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            _ = Task.Run(async () =>
            {
                var commandGuild = client.Guilds.First((guild) => guild.Id == command.GuildId);
                var audioClient = commandGuild.AudioClient;

                using (var ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel panic -i \"F:\\Projects\\Melpominee\\audio\\BGM_EX4_Event_05.mp3\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                }))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var discord = audioClient.CreatePCMStream(AudioApplication.Music))
                {
                    await command.RespondAsync("Pong!", ephemeral: true);
                    try { await output.CopyToAsync(discord); }
                    finally { await discord.FlushAsync(); }
                }
            });
        }

        public SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            return builder;
        }
    }
}

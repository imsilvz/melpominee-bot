using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Melpominee.Interfaces;
using System.Diagnostics;
namespace Melpominee.Commands
{
    public class SummonCommand : ISlashCommandHandler
    {
        public string Name => "summon";
        public string Description => "Summon Melpominee to a channel.";

        public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            IVoiceChannel? voiceChannel = null;
            var channelTarget = command.Data.Options.Where((opt) => opt.Name == "channel").FirstOrDefault();
            if (channelTarget is null) 
            {
                var guildUser = (IGuildUser?)(await command.Channel.GetUserAsync(command.User.Id));
                if (guildUser is not null)
                {
                    voiceChannel = guildUser.VoiceChannel;
                }
            }
            else
            {
                voiceChannel = (IVoiceChannel)channelTarget.Value;
            }

            // break out
            if (voiceChannel is null)
            {
                await command.RespondAsync("You must either be in a voice channel, or specify a voice channel to join!", ephemeral: true);
                return;
            }
            // spawn a discarded task so as not to block command runner
            _ = Task.Run(async () =>
            {
                // connect
                var audioClient = await voiceChannel.ConnectAsync(true, false, false);
                await command.RespondAsync($"Successfully joined voice channel \'{voiceChannel.Name}\'!", ephemeral: true);
            });
        }

        public SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("channel", ApplicationCommandOptionType.Channel, "Voice Channel to connect to.", channelTypes: new List<ChannelType> { ChannelType.Voice }, isRequired: false);
            return builder;
        }
    }
}

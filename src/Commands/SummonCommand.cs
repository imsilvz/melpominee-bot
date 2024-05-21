using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Services;
using System.Diagnostics;
namespace Melpominee.Commands
{
    public class SummonCommand : MelpomineeCommand
    {
        public SummonCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "summon";
        public override string Description => "Summon Melpominee to a channel.";

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
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

            // connect
            if (await _audioService.Connect(voiceChannel))
            {
                await command.RespondAsync($"Successfully joined voice channel \'{voiceChannel.Name}\'!", ephemeral: true);
            }
            else
            {
                await command.RespondAsync("Failed to join voice channel.", ephemeral: true);
            }
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("channel", ApplicationCommandOptionType.Channel, "Voice Channel to connect to.", channelTypes: new List<ChannelType> { ChannelType.Voice }, isRequired: false);
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

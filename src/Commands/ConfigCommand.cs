using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;

namespace Melpominee.src.Commands
{
    public class ConfigCommand : MelpomineeCommand
    {
        public ConfigCommand(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Name => "config";
        public override string Description => "View or modify Melpominee configurations";

        public override Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            throw new NotImplementedException();
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            var configOption = new List<ConfigurationOption>
            {
                new ConfigurationOption
                {
                    Name = "music",
                    Description = "Related to music playback",
                    Options =
                    [
                        new SlashCommandOptionBuilder()
                        .WithName("channel")
                        .WithDescription("The channel where the music modal is hosted")
                        .WithType(ApplicationCommandOptionType.Channel)
                        .AddChannelType(ChannelType.Text)
                    ]
                }
            };

            foreach(var option in configOption) 
            {
                var getChoices = new SlashCommandOptionBuilder()
                    .WithName("option")
                    .WithDescription("The configuration option you wish to get.")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true);

                // populate choices
                foreach (var choice in option.Options)
                {
                    getChoices = getChoices.AddChoice(choice.Name, choice.Name);
                }

                var optionBuilder = new SlashCommandOptionBuilder()
                    .WithName(option.Name)
                    .WithDescription(option.Description)
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOptions([
                        new SlashCommandOptionBuilder()
                            .WithName("get")
                            .WithDescription($"Get setting {option.Description.ToLower()}")
                            .WithType(ApplicationCommandOptionType.SubCommand)
                            .AddOption(getChoices),
                        new SlashCommandOptionBuilder()
                            .WithName("set")
                            .WithDescription($"Set setting {option.Description.ToLower()}")
                            .WithType(ApplicationCommandOptionType.SubCommand)
                            .AddOptions(option.Options)
                    ]);
                builder.AddOption(optionBuilder);
            }
            return builder;
        }
    }
}

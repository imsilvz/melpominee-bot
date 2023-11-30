using Dapper;
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
        public override string Description => "View or modify Melpominee configuration.";

        private readonly List<ConfigurationOption> configurationOptions = new List<ConfigurationOption>
            {
                new ConfigurationOption
                {
                    Name = "music",
                    Description = "Music playback",
                    Options =
                    [
                        new SlashCommandOptionBuilder()
                        .WithName("channel")
                        .WithDescription("Channel where the music modal should be hosted.")
                        .WithType(ApplicationCommandOptionType.Channel)
                        .AddChannelType(ChannelType.Text)
                    ],
                    GetValue = async (dbContext, fieldName) =>
                    {
                        using(var conn = dbContext.Connect())
                        {
                            var reader = await conn.ExecuteReaderAsync("SELECT 1");
                            if (reader.Read())
                                return reader.GetInt32(0);
                        }
                        return "";
                    },
                    SetValue = async (dbContext, fieldName, value) =>
                    {
                        return true;
                    },
                }
            };

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            var optionData = command.Data.Options.First();
            var optionActionData = optionData.Options.First();
            var optionConfig = configurationOptions.Where((opt) => opt.Name == optionData.Name).First();

            if (optionActionData.Name == "get")
            {
                var fieldName = (string)optionActionData.Options.First().Value;
                var fieldValue = (await optionConfig.GetValue(_dataContext, "option")).ToString();
                await command.RespondAsync($"Current {optionData.Name} {fieldName} value: {fieldValue}", ephemeral: true);
            }
            else
            {
                string responseString = "Configuration Changes\n";
                foreach(var field in optionActionData.Options)
                {
                    Console.WriteLine(field.Name);
                    Console.WriteLine(field.Value);
                    if (await optionConfig.SetValue(_dataContext, field.Name, field.Value))
                    {
                        responseString = $"{responseString}- Successfully set {optionData.Name} {field.Name} to {field.Value}\n";
                    }
                    else
                    {
                        responseString = $"{responseString}- Failed to set {optionData.Name} {field.Name} to {field.Value}\n";
                    }
                }
                await command.RespondAsync(responseString, ephemeral: true);
            }
        }

        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            foreach(var option in configurationOptions) 
            {
                var getChoices = new SlashCommandOptionBuilder()
                    .WithName("option")
                    .WithDescription("The configuration option you wish to fetch.")
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
                            .WithDescription($"Get {option.Description.ToLower()} setting.")
                            .WithType(ApplicationCommandOptionType.SubCommand)
                            .AddOption(getChoices),
                        new SlashCommandOptionBuilder()
                            .WithName("set")
                            .WithDescription($"Set {option.Description.ToLower()} setting.")
                            .WithType(ApplicationCommandOptionType.SubCommand)
                            .AddOptions(option.Options)
                    ]);
                builder.AddOption(optionBuilder);
            }
            return builder;
        }
    }
}

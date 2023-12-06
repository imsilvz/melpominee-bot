using Dapper;
using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;
namespace Melpominee.Commands
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
                    GetValue = async (dbContext, guild, optionName, fieldName) =>
                    {
                        using(var conn = dbContext.Connect())
                        {
                            var sql = @"
                                SELECT value
                                FROM melpominee_bot_config
                                WHERE guild = @GuildId
                                  AND setting = @SettingName;
                            ";
                            var reader = await conn.ExecuteReaderAsync(sql, new 
                            { 
                                GuildId = guild.Id.ToString(), 
                                SettingName = $"{optionName}-{fieldName}" 
                            });
                            if (reader.Read())
                            {
                                var dbString = reader.GetString(0);
                                switch(fieldName)
                                {
                                    case "channel":
                                        var guildChannel = guild.GetChannel(ulong.Parse(dbString));
                                        return guildChannel;
                                    default:
                                        return dbString;
                                }
                            }
                        }
                        return null;
                    },
                    SetValue = async (dbContext, guild, optionName, fieldName, value) =>
                    {
                        string storeValue = "null";
                        switch(fieldName)
                        {
                            case "channel":
                                var channelValue = (IGuildChannel)value;
                                storeValue = channelValue.Id.ToString();
                                break;
                            default:
                                storeValue = value is not null ? value.ToString()! : "null";
                                break;
                        }
                        using (var conn = dbContext.Connect())
                        {
                            var sql = @"
                                INSERT INTO melpominee_bot_config
                                    (guild, setting, value)
                                VALUES
                                    (@GuildId, @SettingName, @Value)
                                ON CONFLICT (guild, setting) DO UPDATE
                                SET value = EXCLUDED.value;
                            ";
                            return (await conn.ExecuteAsync(sql, new
                            {
                                GuildId = guild.Id.ToString(),
                                SettingName = $"{optionName}-{fieldName}",
                                Value = storeValue,
                            })) > 0;
                        }
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
                object? fieldValue = await optionConfig.GetValue(_dataContext, client.GetGuild((ulong)command.GuildId!), optionData.Name, fieldName);
                var fieldValueString = fieldValue is null ? "null" : fieldValue.ToString();
                await command.RespondAsync($"Current {optionData.Name} {fieldName} value: {fieldValueString}", ephemeral: true);
            }
            else
            {
                string responseString = "Configuration Changes\n";
                foreach(var field in optionActionData.Options)
                {
                    if (await optionConfig.SetValue(_dataContext, client.GetGuild((ulong)command.GuildId!), optionData.Name, field.Name, field.Value))
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
            builder.WithDMPermission(false);
            return builder;
        }
    }
}

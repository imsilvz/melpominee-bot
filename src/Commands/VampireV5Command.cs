using Discord;
using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;
using Melpominee.Utility;
namespace Melpominee.Commands
{
    public class VampireV5Command : MelpomineeCommand
    {
        public VampireV5Command(DataContext dataContext) : base(dataContext) { }

        public override string Name => "v5";
        public override string Description => "Roll a dice pool from Vampire: the Masquerade v5";

        public override async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            // get params
            var dicePool = (int)(long)command.Data.Options.Where(opt => opt.Name == "pool").First().Value;
            var hungerPool = (int)(long)command.Data.Options.Where(opt => opt.Name == "hunger").First().Value;

            // cannot roll more hunger dice than are available
            if (dicePool < hungerPool) 
            {
                hungerPool = dicePool;
            }
            var results = VTMV5.RollDicePool(dicePool, hungerPool);
            var messageString = VTMV5.GetResultMessage(results.DiceResults, results.HungerResults);

            // build message components
            var componentBuilder = new ComponentBuilder();
            if (results.Reroll == V5DiceResult.RerollType.None)
            {
                // action row one
                var firstRowActionBuilder = new ActionRowBuilder();
                if (results.DiceResults.Where(res => res < 6).Any())
                {
                    firstRowActionBuilder
                        .WithButton("Re-roll Failures", "reroll-failures", ButtonStyle.Secondary, Emote.Parse("<:v5fail:1047224347707838605>"));
                }
                if (results.DiceResults.Where(res => res < 10).Any())
                {
                    firstRowActionBuilder
                        .WithButton("Maximize Crits", "maximize-crits", ButtonStyle.Secondary, Emote.Parse("<:v5crit:1047224346650890260>"));
                }
                componentBuilder.AddRow(firstRowActionBuilder);

                // action row two, only display if we can avoid a messy critical situation! (e.g. more than 0 10s in dice pool, and fewer than 2 in hunger pool)
                if (results.MessyCritical && results.DiceResults.Where(res => res == 10).ToArray().Length > 0 && results.HungerResults.Where(res => res == 10).ToArray().Length < 2)
                {
                    componentBuilder.AddRow(
                        new ActionRowBuilder()
                        .WithButton("Avoid Messy Critical", "avoid-messy", ButtonStyle.Secondary, Emote.Parse("<:v5hungercrit:1047224348638978048>"))
                    );
                }
            }

            // get embed
            results.SourceUser = command.User.Username;
            results.SourceUserIcon = command.User.GetAvatarUrl();
            var embed = VTMV5.GetResultEmbed(results);   
            await command.RespondAsync(messageString, components: componentBuilder.Build(), embed: embed.Build());
        }
        
        public override SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("pool", ApplicationCommandOptionType.Integer, "Number of dice in the pool", isRequired: true);
            builder.AddOption("hunger", ApplicationCommandOptionType.Integer, "Number of dice to replace with hunger dice", isRequired: true);
            return builder;
        }
    }
}

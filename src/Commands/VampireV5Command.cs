using Discord;
using Discord.WebSocket;
using Melpominee.Interfaces;
using Melpominee.Utility;
namespace Melpominee.Commands
{
    public class VampireV5Command : ISlashCommand
    {
        public string Name => "v5";
        public string Description => "Roll a dice pool from Vampire: the Masquerade v5";

        public async Task Execute(DiscordSocketClient client, SocketSlashCommand command)
        {
            // const
            var bestialEmoji = Emote.Parse("<:v5bestial:1047224345375821987>");
            var failEmoji = Emote.Parse("<:v5fail:1047224347707838605>");
            var successEmoji = Emote.Parse("<:v5sux:1047224352686489710>");
            var critEmoji = Emote.Parse("<:v5crit:1047224346650890260>");
            var hungerFailEmoji = Emote.Parse("<:v5hungerfail:1047224350501249034>");
            var hungerSuccessEmoji = Emote.Parse("<:v5hungersux:1047224351470133310>");
            var hungerCritEmoji = Emote.Parse("<:v5hungercrit:1047224348638978048>");

            // get params
            var dicePool = (int)(long)command.Data.Options.Where(opt => opt.Name == "pool").First().Value;
            var hungerPool = (int)(long)command.Data.Options.Where(opt => opt.Name == "hunger").First().Value;

            // cannot roll more hunger dice than are available
            if (dicePool < hungerPool) 
            {
                hungerPool = dicePool;
            }
            var results = VTMV5.RollDicePool(dicePool, hungerPool);

            // build message description
            string bestFailMessage = "";
            string diceFailMessage = "";
            string hungerFailMessage = "";
            string diceSuccessMessage = "";
            string hungerSuccessMessage = "";
            string diceCritMessage = "";
            string hungerCritMessage = "";

            foreach (var res in results.DiceResults)
            {
                if (res < 6)
                {
                    diceFailMessage = $"{diceFailMessage}{failEmoji}";
                }
                else
                {
                    if (res == 10)
                    {
                        diceCritMessage = $"{diceCritMessage}{critEmoji}";
                    }
                    else
                    {
                        diceSuccessMessage = $"{diceSuccessMessage}{successEmoji}";
                    }
                }
            }

            foreach (var res in results.HungerResults)
            {
                if (res < 6)
                {
                    if (res == 1)
                    {
                        bestFailMessage = $"{bestFailMessage}{bestialEmoji}";
                    }
                    else
                    {
                        hungerFailMessage = $"{hungerFailMessage}{hungerFailEmoji}";
                    }
                }
                else
                {
                    if (res == 10)
                    {
                        hungerCritMessage = $"{hungerCritMessage}{hungerCritEmoji}";
                    }
                    else
                    {
                        hungerSuccessMessage = $"{hungerSuccessMessage}{hungerSuccessEmoji}";
                    }
                }
            }
            string messageString = $"{bestFailMessage}{hungerFailMessage}{diceFailMessage}{diceSuccessMessage}{hungerSuccessMessage}{hungerCritMessage}{diceCritMessage}";

            // get embed
            var embed = VTMV5.GetResultEmbed(command, results);   
            await command.RespondAsync(messageString, embed: embed.Build());
        }

        public SlashCommandBuilder Register(DiscordSocketClient client, SlashCommandBuilder builder)
        {
            builder.AddOption("pool", ApplicationCommandOptionType.Integer, "Number of dice in the pool", isRequired: true);
            builder.AddOption("hunger", ApplicationCommandOptionType.Integer, "Number of dice to replace with hunger dice", isRequired: true);
            return builder;
        }
    }
}

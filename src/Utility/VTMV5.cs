using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Melpominee.Models;
namespace Melpominee.Utility
{
    public static class VTMV5
    {
        public static V5DiceResult CalculateSuccesses(int[] dicePool, int[] hungerPool)
        {
            int successes = 0;
            bool nextPairIsCritical = false;
            bool bestial = false;
            bool critical = false;
            bool messy = false;
            foreach(var res in dicePool) 
            { 
                if (res >= 6)
                {
                    successes++;
                    if (res == 10)
                    {
                        if (nextPairIsCritical)
                        {
                            // a critical pair is worth 4
                            successes += 2;
                            nextPairIsCritical = false;
                            critical = true;
                        }
                        else
                        {
                            nextPairIsCritical = true;
                        }
                    }
                }
            }
            foreach(var res in hungerPool) 
            {
                if (res >= 6)
                {
                    successes++;
                    if (res == 10)
                    {
                        if (nextPairIsCritical)
                        {
                            // a critical pair is worth 4
                            successes += 2;
                            nextPairIsCritical = false;
                            critical = true;
                            messy = true;
                        }
                        else
                        {
                            nextPairIsCritical = true;
                        }
                    }
                }
                else if (res == 1)
                {
                    // bestial?
                }
            }
            return new V5DiceResult
            {
                Successes = successes,
                DiceResults = dicePool,
                HungerResults = hungerPool,
                Reroll = false,
                BestialFailure = bestial,
                Critical = critical,
                MessyCritical = messy,
            };
        }

        public static EmbedBuilder GetResultEmbed(SocketSlashCommand command, V5DiceResult result)
        {
            // const
            Color defaultColor = new Color(2, 2, 2);
            Color critColor = new Color(96, 0, 0);
            Color messyColor = new Color(255, 0, 0);

            int dicePool = result.DiceResults.Length + result.HungerResults.Length;
            int hungerPool = result.HungerResults.Length;

            // build message title
            var messageColor = defaultColor;
            var messageTitle = $"{command.User.Username} rolled | Pool {dicePool}";
            if (hungerPool > 0)
            {
                messageTitle = $"{messageTitle} | Hunger {hungerPool}";
            }


            // build message fields
            // result
            string resultText = $"Total: **{result.Successes}**";
            if (result.Successes == 1)
            {
                // can't be a crit with only one success
                resultText = $"{resultText} success";
                resultText = $"{resultText}\n**[ Success! ]**";
            }
            else
            {
                resultText = $"{resultText} successes";

                if (result.Successes == 0)
                {
                    if (result.BestialFailure)
                    {
                        resultText = $"{resultText}\n**[ Bestial Failure! ]**";
                    }
                    else
                    {
                        resultText = $"{resultText}\n**[ Total Failure! ]**";
                    }
                }
                else if(result.Critical)
                {
                    if (result.MessyCritical)
                    {
                        messageColor = messyColor;
                        resultText = $"{resultText}\n**[ Messy Critical! ]**";
                    }
                    else
                    {
                        messageColor = critColor;
                        resultText = $"{resultText}\n**[ Critical Success! ]**";
                    }
                }
                else
                {
                    resultText = $"{resultText}\n**[ Success! ]**";
                }
            }

            // dice
            int[] successDice = result.DiceResults.Where(dice => dice >= 6).Order().ToArray();
            int[] failureDice = result.DiceResults.Where(dice => dice < 6).Order().ToArray();
            string diceString = $"```{string.Join(' ', failureDice)} | {string.Join(' ', successDice)}```";

            // hunger
            string hungerString = "";
            int[] successHunger = result.HungerResults.Where(dice => dice >= 6).Order().ToArray();
            int[] failureHunger = result.HungerResults.Where(dice => dice < 6).Order().ToArray();
            hungerString = $"```diff\n- {string.Join(' ', failureHunger)} | {string.Join(' ', successHunger)} -```";


            // build embed
            var embed = new EmbedBuilder
            {
                Color = messageColor,
                Author = new EmbedAuthorBuilder
                {
                    Name = messageTitle,
                    IconUrl = command.User.GetAvatarUrl(),
                }
            };
            embed.AddField("Result", resultText, inline: true);
            embed.AddField("Dice", diceString, inline: true);
            if (result.HungerResults.Length > 0)
            {
                embed.AddField("Hunger", hungerString, inline: true);
            }
            return embed;
        }

        public static V5DiceResult RollDicePool(int pool, int hunger)
        {
            Random rand = new Random();
            int[] results = Enumerable
                .Repeat(1, pool - hunger)
                .Select(i => rand.Next(1, 11))
                .ToArray();
            int[] hungerResults = Enumerable
                .Repeat(1, hunger)
                .Select(i => rand.Next(1, 11))
                .ToArray();
            return CalculateSuccesses(results, hungerResults);
        }

        public static V5DiceResult RerollDicePool(V5DiceResult result)
        {
            result.Reroll = true;
            return result;
        }
    }
}

using Melpominee.Models;
using NetCord;
using NetCord.Rest;

namespace Melpominee.Utility;

public static class VTMV5
{
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

    public static V5DiceResult RerollDicePool(V5DiceResult result, V5DiceResult.RerollType rerollType)
    {
        Random rand = new Random();

        int rerollCounter = 0;
        int maxRerolledDice = 3;
        int[] diceResults = result.DiceResults.Order().ToArray();
        switch (rerollType)
        {
            case V5DiceResult.RerollType.RerollFailures:
                {
                    for (int i = 0; i < diceResults.Length; i++)
                    {
                        if (rerollCounter >= maxRerolledDice)
                            break;

                        int die = diceResults[i];
                        if (die < 6)
                        {
                            diceResults[i] = rand.Next(1, 11);
                            rerollCounter++;
                        }
                    }
                    diceResults = diceResults.Order().ToArray();
                    break;
                }
            case V5DiceResult.RerollType.MaximizeCrits:
                {
                    for (int i = 0; i < diceResults.Length; i++)
                    {
                        if (rerollCounter >= maxRerolledDice)
                            break;

                        int die = diceResults[i];
                        if (die < 10)
                        {
                            diceResults[i] = rand.Next(1, 11);
                            rerollCounter++;
                        }
                    }
                    diceResults = diceResults.Order().ToArray();
                    break;
                }
            case V5DiceResult.RerollType.AvoidMessy:
                {
                    diceResults = diceResults.OrderDescending().ToArray();
                    for (int i = 0; i < diceResults.Length; i++)
                    {
                        if (rerollCounter >= maxRerolledDice)
                            break;

                        int die = diceResults[i];
                        if (die == 10)
                        {
                            diceResults[i] = rand.Next(1, 11);
                            rerollCounter++;
                        }
                    }
                    break;
                }
            default:
                break;
        }
        var newResult = CalculateSuccesses(diceResults, result.HungerResults);

        newResult.Reroll = rerollType;
        return newResult;
    }

    public static string GetResultMessage(int[] dicePool, int[] hungerPool)
    {
        // const
        var bestialEmoji = "<:v5bestial:1047224345375821987>";
        var failEmoji = "<:v5fail:1047224347707838605>";
        var successEmoji = "<:v5sux:1047224352686489710>";
        var critEmoji = "<:v5crit:1047224346650890260>";
        var hungerFailEmoji = "<:v5hungerfail:1047224350501249034>";
        var hungerSuccessEmoji = "<:v5hungersux:1047224351470133310>";
        var hungerCritEmoji = "<:v5hungercrit:1047224348638978048>";

        // build message description
        string bestFailMessage = "";
        string diceFailMessage = "";
        string hungerFailMessage = "";
        string diceSuccessMessage = "";
        string hungerSuccessMessage = "";
        string diceCritMessage = "";
        string hungerCritMessage = "";

        foreach (var res in dicePool)
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

        foreach (var res in hungerPool)
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
        return $"{bestFailMessage}{hungerFailMessage}{diceFailMessage}{diceSuccessMessage}{hungerSuccessMessage}{hungerCritMessage}{diceCritMessage}";
    }

    public static EmbedProperties GetResultEmbed(V5DiceResult result)
    {
        // const
        Color defaultColor = new Color(2, 2, 2);
        Color critColor = new Color(96, 0, 0);
        Color messyColor = new Color(255, 0, 0);

        int dicePool = result.DiceResults.Length + result.HungerResults.Length;
        int hungerPool = result.HungerResults.Length;

        // build message title
        var messageColor = defaultColor;
        var messageTitle = $"{result.SourceUser} rolled | Pool {dicePool}";
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
            else if (result.Critical)
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
        var embed = new EmbedProperties();
        embed.Color = messageColor;
        embed.Author = new()
        {
            Name = messageTitle,
            IconUrl = result.SourceUserIcon,
        };
        embed.AddFields([
            new()
            {
                Name = "Result",
                Value = resultText,
                Inline = true
            },
            new()
            {
                Name = "Dice",
                Value = diceString,
                Inline = true
            }
        ]);

        // add hunger if applicable
        if (result.HungerResults.Length > 0)
        {
            embed.AddFields([
                new()
                {
                    Name = "Hunger",
                    Value = hungerString,
                    Inline = true
                },
            ]);
        }

        return embed;
    }

    private static V5DiceResult CalculateSuccesses(int[] dicePool, int[] hungerPool)
    {
        int successes = 0;
        bool nextPairIsCritical = false;
        bool bestial = false;
        bool critical = false;
        bool messy = false;
        foreach (var res in dicePool)
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
        foreach (var res in hungerPool)
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
            Reroll = V5DiceResult.RerollType.None,
            BestialFailure = bestial,
            Critical = critical,
            MessyCritical = messy,
        };
    }
}

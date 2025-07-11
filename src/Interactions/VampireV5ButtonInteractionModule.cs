using Melpominee.Models;
using Melpominee.Utility;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Melpominee.Interactions;
public class VampireV5ButtonInteractionModule : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("avoid-messy")]
    public async Task AvoidMessyCriticals()
    {
        await DoReroll(V5DiceResult.RerollType.AvoidMessy);
    }

    [ComponentInteraction("maximize-crits")]
    public async Task MaximizeCriticals()
    {
        await DoReroll(V5DiceResult.RerollType.MaximizeCrits);
    }

    [ComponentInteraction("reroll-failures")]
    public async Task RerollFailures()
    {
        await DoReroll(V5DiceResult.RerollType.RerollFailures);
    }

    private async Task DoReroll(V5DiceResult.RerollType rerollType)
    {
        var interaction = Context.Interaction;
        var messageEmbed = Context.Message.Embeds.First();
        if (messageEmbed is null)
        {
            await interaction.SendResponseAsync(
                InteractionCallback.Message(
                    new()
                    {
                        Content = "Oops! Something went wrong.",
                        Flags = MessageFlags.Ephemeral
                    }
                )
            );
            return;
        }

        string? diceString = messageEmbed.Fields.FirstOrDefault((field) => field.Name == "Dice")?.Value;
        string? hungerString = messageEmbed.Fields.FirstOrDefault((field) => field.Name == "Hunger")?.Value;

        // parse dice
        V5DiceResult result = new V5DiceResult();
        if (diceString is not null)
        {
            diceString = diceString
                .Replace(" | ", " ")
                .Replace("`", "")
                .Trim();
            result.DiceResults = diceString
                .Split(" ")
                .Select(int.Parse)
                .ToArray();
        }
        else
        {
            result.DiceResults = Array.Empty<int>();
        }

        // parse hunger
        if (hungerString is not null)
        {
            hungerString = hungerString
                .Replace(" | ", " ")
                .Replace("`", "")
                .Replace("diff", "")
                .Replace("-", "")
                .Trim();
            result.HungerResults = hungerString
                .Split(" ")
                .Select(int.Parse)
                .ToArray();
        }
        else
        {
            result.HungerResults = Array.Empty<int>();
        }

        // build result message
        result = VTMV5.RerollDicePool(result, rerollType);
        result.SourceUser = interaction.User.Username;
        result.SourceUserIcon = interaction.User.GetAvatarUrl()?.ToString() ?? string.Empty;
        var resultMessage = VTMV5.GetResultMessage(result.DiceResults, result.HungerResults);
        var resultEmbed = VTMV5.GetResultEmbed(result);

        // create interaction message
        var message = new InteractionMessageProperties
        {
            Content = resultMessage,
            Embeds = new[] { resultEmbed },
        };
        await interaction.SendResponseAsync(InteractionCallback.Message(message));
    }
}

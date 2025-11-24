using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Melpominee.Utility;

namespace Melpominee.Commands;
public class VampireV5CommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("v5", "Roll a dice pool from Vampire: the Masquerade v5", Register = true)]
    public async Task VampireV5Roll(
        [SlashCommandParameter(Name = "pool", Description = "Number of dice in the pool")] int @dicePool,
        [SlashCommandParameter(Name = "hunger", Description = "Number of dice to replace with hunger dice")] int @hungerPool)
    {
        var interaction = Context.Interaction;
        // await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        // cannot roll more hunger dice than are available
        if (dicePool < hungerPool)
        {
            hungerPool = dicePool;
        }
        var results = VTMV5.RollDicePool(dicePool, hungerPool);
        var messageString = VTMV5.GetResultMessage(results.DiceResults, results.HungerResults);

        // populate embed information
        results.SourceUser = Context.User.Username;
        results.SourceUserIcon = Context.User.GetAvatarUrl()?.ToString() ?? string.Empty;
        var embed = VTMV5.GetResultEmbed(results);

        // create components
        var embedComponents = new List<IMessageComponentProperties>();

        // first action row
        var firstRow = new ActionRowProperties();
        if (results.DiceResults.Where(res => res < 6).Any())
        {
            firstRow.AddComponents([
                new ButtonProperties("reroll-failures", "Re-roll Failures", EmojiProperties.Custom(1047224347707838605), ButtonStyle.Secondary)
            ]);
        }
        if (results.DiceResults.Where(res => res < 10).Any())
        {
            firstRow.AddComponents([
                new ButtonProperties("maximize-crits", "Maximize Crits", EmojiProperties.Custom(1047224346650890260), ButtonStyle.Secondary)
            ]);
        }

        // action row two, only display if we can avoid a messy critical situation! (e.g. more than 0 10s in dice pool, and fewer than 2 in hunger pool)
        var secondRow = new ActionRowProperties();
        if (results.MessyCritical && results.DiceResults.Where(res => res == 10).ToArray().Length > 0 && results.HungerResults.Where(res => res == 10).ToArray().Length < 2)
        {
            secondRow.AddComponents([
                new ButtonProperties("avoid-messy", "Avoid Messy Critical", EmojiProperties.Custom(1047224348638978048), ButtonStyle.Secondary)
            ]);
        }

        // add action rows if applicable
        if (firstRow.Components.Count() > 0)
            embedComponents.Add(firstRow);
        if (secondRow.Components.Count() > 0)
            embedComponents.Add(secondRow);

        // create interaction message
        var message = new InteractionMessageProperties
        {
            Content = messageString,
            Components = embedComponents.ToArray(),
            Embeds = [embed],
        };
        await interaction.SendResponseAsync(InteractionCallback.Message(message));
    }
}


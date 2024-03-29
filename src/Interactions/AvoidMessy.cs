﻿using Discord.WebSocket;
using Melpominee.Abstractions;
using Melpominee.Models;
using Melpominee.Services;
using Melpominee.Utility;
namespace Melpominee.Interactions
{
    public class AvoidMessy : MelpomineeInteraction
    {
        public AvoidMessy(AudioService audioService, DataContext dataContext) : base(audioService, dataContext) { }

        public override string Id => "avoid-messy";
        public override async Task Execute(DiscordSocketClient client, SocketInteraction interaction)
        {
            var messageData = (SocketMessageComponent)interaction;
            var messageEmbed = messageData.Message.Embeds.First();
            string? diceString = messageEmbed.Fields.FirstOrDefault((field) => field.Name == "Dice").Value;
            string? hungerString = messageEmbed.Fields.FirstOrDefault((field) => field.Name == "Hunger").Value;

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
                result.DiceResults = new int[0];
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
                result.HungerResults = new int[0];
            }

            // build result message
            result = VTMV5.RerollDicePool(result, V5DiceResult.RerollType.AvoidMessy);
            result.SourceUser = interaction.User.Username;
            result.SourceUserIcon = interaction.User.GetAvatarUrl();
            var resultMessage = VTMV5.GetResultMessage(result.DiceResults, result.HungerResults);
            var resultEmbed = VTMV5.GetResultEmbed(result);
            await interaction.RespondAsync(resultMessage, embed: resultEmbed.Build());
        }
    }
}

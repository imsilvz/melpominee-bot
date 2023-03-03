import { RerollDicePool } from "./dice.js";
import { SendV5Reply } from "./util.js";

const validIds = [
    'reroll-failure',
    'maximize-crits',
    'avoid-messy'
]

const idMap = {
    'reroll-failure': 'Re-roll Failures',
    'maximize-crits': 'Maximize Crits',
    'avoid-messy': 'Avoid Messy Crits'
}

export const handleButtonPress = async (interaction) => {
    let embed = interaction.message.embeds[0];
    let diceField = embed.data.fields[1];
    let hungerField = embed.data.fields[2];
    let diceString = diceField.value.substring(3, diceField.value.length - 3).trim();
    let hungerString = hungerField ? hungerField.value.substring(9, hungerField.value.length - 4).trim() : ' | ';
    
    let diceSplit = diceString.split('|');
    let hungerSplit = hungerString.split('|');
    let lastDice = [diceSplit[0].trim(), diceSplit[1].trim()].join(' ').split(' ').filter((el) => el != '').map(Number);
    let lastHungerDice = [hungerSplit[0].trim(), hungerSplit[1].trim()].join(' ').split(' ').filter((el) => el != '').map(Number);

    // assemble additional params
    let pool = lastDice.length + lastHungerDice.length;
    let hunger = lastHungerDice.length;

    // reroll dice
    if(interaction.user.id == interaction.message.interaction.user.id) {
        if(validIds.includes(interaction.customId)) {
            const { successes, dice, hungerDice, bestial, messy, critical } = await RerollDicePool(lastDice, lastHungerDice, interaction.customId);
            await SendV5Reply(interaction, pool, hunger, null, successes, dice, hungerDice, bestial, messy, critical, true, idMap[interaction.customId]);
        } else {
            interaction.reply({
                content: `Sorry, this function (${interaction.customId}) is not yet implemented!`,
                ephemeral: false
            });
        }
    } else {
        interaction.reply({
            content: `Sorry, you can only reroll your own rolls!`,
            ephemeral: true
        });
    }
}
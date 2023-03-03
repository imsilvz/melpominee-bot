export const RollDicePool = async (pool, hunger) => {
    let successes = 0;
    let messy = false;
    let bestial = false;
    let critical = false;
    let dicePool = Array.from({length: pool - hunger}, () => Math.floor(Math.random() * 10) + 1).sort((a, b) => {
        return a - b;
    });
    let hungerPool = Array.from({length: hunger}, () => Math.floor(Math.random() * 10) + 1).sort((a, b) => {
        return a - b;
    });

    let critCount = 0;
    for(let i=0; i<dicePool.length; i++) {
        let roll = dicePool[i];
        if(roll == 10) {
            critCount++;
            if(critCount % 2 == 0) {
                // each pair of crits is worth 4
                successes += 3;
                critical = true;
            } else {
                successes++;
            }
        } else if (roll >= 6) {
            successes++;
        }
    }

    for(let i=0; i<hungerPool.length; i++) {
        let roll = hungerPool[i];
        if(roll == 10) {
            critCount++;
            if(critCount % 2 == 0) {
                // each pair of crits is worth 4
                successes += 3;
                critical = true;
            } else {
                successes++;
            }
            messy = true;
        } else if (roll >= 6) {
            successes++;
        } else if(roll == 1) {
            bestial = true;
        }
    }

    return {
        successes: successes,
        dice: dicePool,
        hungerDice: hungerPool,
        bestial,
        messy,
        critical
    }
}

export const RerollDicePool = async (dice, hunger, rerollType) => {
    let successes = 0;
    let messy = false;
    let bestial = false;
    let critical = false;

    let counter = 0;
    let newDice = [];
    switch(rerollType) {
        case 'avoid-messy':
            // reroll criticals to avoid a messy crit
            for(let i=0; i<dice.length; i++) {
                let roll = dice[i];
                if(counter < 3) {
                    if(roll != 10) {
                        newDice.push(roll);
                    } else {
                        counter++;
                    }
                } else {
                    newDice.push(roll);
                }
            }
            newDice.push(...Array.from({length: dice.length - newDice.length}, () => Math.floor(Math.random() * 10) + 1));
            newDice = newDice.sort((a, b) => {
                return a - b;
            });
            break;
        case 'maximize-crits':
            // reroll non successes first
            for(let i=0; i<dice.length; i++) {
                let roll = dice[i];
                if(roll >= 6) {
                    newDice.push(roll);
                } else {
                    if(counter >= 3) {
                        // only reroll up to 3
                        newDice.push(roll);
                    }
                    counter++;
                }
            }
            
            // reroll additional dice, searching for crit
            let temp = [];
            for(let i=0; i<newDice.length; i++) {
                if(counter < 3) {
                    let roll = newDice[i];
                    if(roll != 10) {
                        counter++;
                    } else {
                        temp.push(roll);
                    }
                } else {
                    temp.push(roll);
                }
            }
            newDice = temp;

            newDice.push(...Array.from({length: dice.length - newDice.length}, () => Math.floor(Math.random() * 10) + 1));
            newDice = newDice.sort((a, b) => {
                return a - b;
            });
            break;
        default:
            for(let i=0; i<dice.length; i++) {
                let roll = dice[i];
                if(roll >= 6) {
                    newDice.push(roll);
                } else {
                    if(counter >= 3) {
                        // only reroll up to 3
                        newDice.push(roll);
                    }
                    counter++;
                }
            }
            newDice.push(...Array.from({length: dice.length - newDice.length}, () => Math.floor(Math.random() * 10) + 1));
            newDice = newDice.sort((a, b) => {
                return a - b;
            });
    }

    let critCount = 0;
    let dicePool = [...newDice];
    let hungerPool = [...hunger];
    for(let i=0; i<dicePool.length; i++) {
        let roll = dicePool[i];
        if(roll == 10) {
            critCount++;
            if(critCount % 2 == 0) {
                // each pair of crits is worth 4
                successes += 3;
                critical = true;
            } else {
                successes++;
            }
        } else if (roll >= 6) {
            successes++;
        }
    }

    for(let i=0; i<hungerPool.length; i++) {
        let roll = hungerPool[i];
        if(roll == 10) {
            critCount++;
            if(critCount % 2 == 0) {
                // each pair of crits is worth 4
                successes += 3;
                critical = true;
            } else {
                successes++;
            }
            messy = true;
        } else if (roll >= 6) {
            successes++;
        } else if(roll == 1) {
            bestial = true;
        }
    }

    return {
        successes: successes,
        dice: dicePool,
        hungerDice: hungerPool,
        bestial,
        messy,
        critical
    }
}
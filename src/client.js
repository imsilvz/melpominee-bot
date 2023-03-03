// Require the necessary discord.js classes
import { 
	Client, GatewayIntentBits
} from 'discord.js';

// Create a new client instance
const client = new Client({ intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildVoiceStates] });
export default client;
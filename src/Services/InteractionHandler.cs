using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melpominee.Services
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        public InteractionHandler(DiscordSocketClient client, InteractionService interactions)
        {
            _interactions = interactions;
            _client = client;
        }
    }
}

﻿using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melpominee.Interfaces
{
    public interface IInteractionHandler
    {
        public string Id { get; }
        public Task Execute(DiscordSocketClient client, SocketInteraction interaction);
    }
}

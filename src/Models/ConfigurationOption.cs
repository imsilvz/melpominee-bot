using Discord;
using Discord.WebSocket;
using Melpominee.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melpominee.Models
{
    public class ConfigurationOption
    {

        public required string Name { get; set; }
        public required string Description { get; set; }
        public required SlashCommandOptionBuilder[] Options { get; set; }
        public required Func<DataContext, SocketGuild, string, string, Task<object?>> GetValue;
        public required Func<DataContext, SocketGuild, string, string, object, Task<bool>> SetValue;
    }
}

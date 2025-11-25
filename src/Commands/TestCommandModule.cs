using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Melpominee.Utility;

namespace Melpominee.Commands;
public class TestCommandModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ping", "Pong", Register = true)]
    public async Task Ping()
    {
        await RespondAsync(
            InteractionCallback.Message(
                new()
                {
                    Content = "Pong!",
                    Flags = MessageFlags.Ephemeral
                }
            )
        );
        return;
    }
}

using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Intent;

public record IntentReceiveMessage : IIntent
{
    public required ChatMessage Message { get; init; }
}

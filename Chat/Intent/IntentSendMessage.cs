using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Intent;

public record IntentSendMessage : IIntent
{
    public required ChatMessage Message { get; init; }
}

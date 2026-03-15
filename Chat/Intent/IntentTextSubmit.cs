namespace lemonSpire2.Chat.Intent;

public record IntentTextSubmit : IIntent
{
    public required string Text { get; init; }
}

// ========== Tooltip Intents ==========

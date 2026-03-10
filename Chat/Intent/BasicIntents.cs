using Godot;
using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Intent;

public record IntentSubmit : IIntent
{
    public required ChatMessage Message { get; init; }
}

public record IntentSendMessage : IIntent
{
    public required ChatMessage Message { get; init; }
}

public record IntentReceiveMessage : IIntent
{
    public required ChatMessage Message { get; init; }
}

// ========== Tooltip Intents ==========

public record IntentMetaHoverStart : IIntent
{
    public required string Meta { get; init; }
    public required Vector2 GlobalPosition { get; init; }
}

public record IntentMetaHoverEnd : IIntent
{
    public required string Meta { get; init; }
}

public record IntentMetaClick : IIntent
{
    public required string Meta { get; init; }
}
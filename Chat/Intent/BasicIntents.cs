using Godot;
using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Intent;

public record IntentTextSubmit : IIntent
{
    public required string Text { get; init; }
}

public record IntentSendSegments : IIntent
{
    public ulong? senderId { get; init; } // 0 for system, null to autofill, (also can be specified ? but shouldn't?
    public ulong? receiverId { get; init; } // 0 for broadcast, null to autofill
    public required IReadOnlyCollection<IMsgSegment> Segments { get; init; }
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

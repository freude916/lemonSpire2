using Godot;

namespace lemonSpire2.Chat.Intent;

public record IntentMetaHoverStart : IIntent
{
    public required string Meta { get; init; }
    public required Vector2 GlobalPosition { get; init; }
}

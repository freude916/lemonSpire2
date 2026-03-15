namespace lemonSpire2.Chat.Intent;

public record IntentMetaHoverEnd : IIntent
{
    public required string Meta { get; init; }
}

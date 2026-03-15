namespace lemonSpire2.Chat.Intent;

public record IntentMetaClick : IIntent
{
    public required string Meta { get; init; }
}

namespace lemonSpire2.Chat.Input.Model;

public sealed record ChatCompletionItem(string DisplayText, string InsertText, string? PreviewText = null);

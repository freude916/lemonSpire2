namespace lemonSpire2.Chat.Input;

public static class ChatInputInsertion
{
    public static ChatInputInsertionResult InsertToken(string text, int caretColumn, string token)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var safeCaret = Math.Clamp(caretColumn, 0, text.Length);
        var insertion = BuildInsertion(text, safeCaret, token);
        return new ChatInputInsertionResult(
            text.Insert(safeCaret, insertion),
            safeCaret + insertion.Length);
    }

    private static string BuildInsertion(string text, int caretColumn, string token)
    {
        var needsLeadingSpace = caretColumn > 0 && !char.IsWhiteSpace(text[caretColumn - 1]);
        var needsTrailingSpace = caretColumn >= text.Length || !char.IsWhiteSpace(text[caretColumn]);
        return $"{(needsLeadingSpace ? " " : "")}{token}{(needsTrailingSpace ? " " : "")}";
    }
}

public sealed record ChatInputInsertionResult(string Text, int CaretColumn);

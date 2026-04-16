using lemonSpire2.Chat.Input.Abstractions;

namespace lemonSpire2.Chat.Input.Model;

public sealed record ChatCompletionSession(
    int ReplaceStart,
    int ReplaceLength,
    string Query,
    IChatCompletionProvider Provider);

using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Input.Registry;

namespace lemonSpire2.Chat.Input.Service.Bracket;

public sealed class InlineReferenceCompletionProvider(ChatInlineReferenceRegistry inlineReferences)
    : IChatCompletionProvider
{
    public IReadOnlyList<ChatCompletionItem> GetItems(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var colonIndex = query.IndexOf(':');
        if (colonIndex < 0)
            return inlineReferences.All
                .Where(type => type.TypeName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(type => new ChatCompletionItem(type.TypeName, $"[{type.TypeName}:"))
                .ToList();

        var typeName = query[..colonIndex];
        var payload = query[(colonIndex + 1)..];
        if (!inlineReferences.TryGet(typeName, out var type) || type is null)
            return [];

        return type.GetCompletions(payload);
    }
}

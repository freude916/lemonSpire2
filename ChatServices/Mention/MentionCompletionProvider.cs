using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Service.Mention;

public sealed class MentionCompletionProvider(Func<IReadOnlyList<MentionTarget>> getMentionTargets)
    : IChatCompletionProvider
{
    public IReadOnlyList<ChatCompletionItem> GetItems(string query)
    {
        ArgumentNullException.ThrowIfNull(getMentionTargets);
        return getMentionTargets()
            .Where(target =>
                target.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                target.MentionText.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(target => new ChatCompletionItem(target.DisplayName, $"@{target.MentionText}"))
            .ToList();
    }
}

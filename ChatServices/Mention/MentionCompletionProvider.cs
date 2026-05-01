using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Input.Service.Mention;

public sealed class MentionCompletionProvider(Func<IReadOnlyList<MentionTarget>> getMentionTargets)
    : IChatCompletionProvider
{
    public IReadOnlyList<ChatCompletionItem> GetItems(string query)
    {
        ArgumentNullException.ThrowIfNull(getMentionTargets);
        return
        [
            .. getMentionTargets()
                .Where(target =>
                    // 同时按展示名和 alias 搜索，这样重名后缀不会牺牲原始名字的可发现性。
                    target.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    target.MentionText.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(target => new ChatCompletionItem(target.GetDisplayText(), $"@{target.MentionText} "))
        ];
    }
}

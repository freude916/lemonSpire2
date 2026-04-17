using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.Chat.Input.Service.Bracket;

internal readonly record struct CardCompletionCandidate(string Title, string Entry);

public sealed class CardInlineReference : IChatInlineReference
{
    public string TypeName => "card";

    public IReadOnlyList<ChatCompletionItem> GetCompletions(string query)
    {
        return BuildCompletionItems(
            ModelDb.AllCards.Select(card => new CardCompletionCandidate(card.Title, card.Id.Entry)),
            query);
    }

    public bool TryResolve(string payload, out IMsgSegment segment)
    {
        segment = null!;

        var card = StsUtil.ResolveModel<CardModel>(payload) ??
                   ModelDb.AllCards.SingleOrDefault(model =>
                       string.Equals(model.Title, payload, StringComparison.OrdinalIgnoreCase));
        if (card is null)
            return false;

        segment = CardTooltip.FromChatReference(card).ToTooltipSegment();
        return true;
    }

    internal static IReadOnlyList<ChatCompletionItem> BuildCompletionItems(
        IEnumerable<CardCompletionCandidate> candidates,
        string query)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(query);

        return
        [
            .. candidates
                .Where(candidate => Matches(candidate, query))
                .OrderBy(candidate => candidate.Entry, StringComparer.OrdinalIgnoreCase)
                .Select(candidate =>
                    new ChatCompletionItem($"{candidate.Title} - {candidate.Entry}", $"<card:{candidate.Entry}>"))
        ];
    }

    private static bool Matches(CardCompletionCandidate candidate, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return candidate.Entry.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               candidate.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}

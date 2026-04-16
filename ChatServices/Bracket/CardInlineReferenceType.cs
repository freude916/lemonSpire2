using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.Chat.Input.Service.Bracket;

public sealed class CardInlineReferenceType : IChatInlineReferenceType
{
    public string TypeName => "card";

    public IReadOnlyList<ChatCompletionItem> GetCompletions(string query)
    {
        return
        [
            .. ModelDb.AllCards
                .Where(card => Matches(card, query))
                .OrderBy(card => card.Id.Entry, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .Select(card => new ChatCompletionItem($"{card.Title} - {card.Id.Entry}", $"[card:{card.Id.Entry}]"))
        ];
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

    private static bool Matches(CardModel card, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return card.Id.Entry.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               card.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}

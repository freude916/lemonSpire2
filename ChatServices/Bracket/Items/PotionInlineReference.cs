using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.Chat.Input.Service.Bracket;

public sealed class PotionInlineReference : IChatInlineReference
{
    public string TypeName => "potion";

    public IReadOnlyList<ChatCompletionItem> GetCompletions(string query)
    {
        return BuildCompletionItems(
            ModelDb.AllPotions.Select(potion =>
                new PotionCompletionCandidate(potion.Title.GetFormattedText(), potion.Id.Entry)),
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
        IEnumerable<PotionCompletionCandidate> candidates,
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
                    new ChatCompletionItem($"{candidate.Title} - {candidate.Entry}", $"<potion:{candidate.Entry}>"))
        ];
    }

    private static bool Matches(PotionCompletionCandidate candidate, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return candidate.Entry.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               candidate.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}

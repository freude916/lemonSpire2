using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Input.Model;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.Chat.Input.Service.Bracket;

public sealed class RelicInlineReference : IChatInlineReference
{
    public string TypeName => "relic";

    public IReadOnlyList<ChatCompletionItem> GetCompletions(string query)
    {
        return BuildCompletionItems(
            ModelDb.AllRelics.Select(potion =>
                new RelicCompletionCandidate(potion.Title.GetFormattedText(), potion.Id.Entry)),
            query);
    }

    public bool TryResolve(string payload, out IMsgSegment segment)
    {
        segment = null!;

        var relic = StsUtil.ResolveModel<RelicModel>(payload) ??
                    ModelDb.AllRelics.SingleOrDefault(model =>
                        string.Equals(model.Title.GetFormattedText(), payload, StringComparison.OrdinalIgnoreCase));
        if (relic is null)
            return false;

        segment = RelicTooltip.FromModel(relic).ToTooltipSegment();
        return true;
    }

    internal static IReadOnlyList<ChatCompletionItem> BuildCompletionItems(
        IEnumerable<RelicCompletionCandidate> candidates,
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
                    new ChatCompletionItem($"{candidate.Title} - {candidate.Entry}", $"<relic:{candidate.Entry}>"))
        ];
    }

    private static bool Matches(RelicCompletionCandidate candidate, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        return candidate.Entry.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               candidate.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}

internal readonly record struct RelicCompletionCandidate(string Title, string Entry);

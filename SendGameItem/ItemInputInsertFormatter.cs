using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;

namespace lemonSpire2.SendGameItem;

public static class ItemInputInsertFormatter
{
    public static bool TryFormat(IMsgSegment? segment, out string text)
    {
        text = string.Empty;
        if (segment is not TooltipSegment { Tooltip: var tooltip })
            return false;

        switch (tooltip)
        {
            case CardTooltip cardTooltip:
            {
                var entry = cardTooltip.Snapshot.Id?.Entry;
                if (string.IsNullOrWhiteSpace(entry))
                    return false;

                text = $"<card:{entry}>";
                return true;
            }
            case PotionTooltip { ModelIdStr: { Length: > 0 } modelId }:
                text = $"<potion:{modelId}>";
                return true;
            case RelicTooltip { ModelIdStr: { Length: > 0 } modelId }:
                text = $"<relic:{modelId}>";
                return true;
            default:
                return false;
        }
    }
}

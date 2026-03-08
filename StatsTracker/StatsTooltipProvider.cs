using System.Reflection;
using lemonSpire2.PlayerTooltip;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;

namespace lemonSpire2.StatsTracker;

public class StatsTooltipProvider : ITooltipProvider
{
    public string Id => "lemonSpire2.stats";
    public int Priority => 100;

    private static readonly FieldInfo? TitleField =
        typeof(HoverTip).GetField("<Title>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? DescriptionField =
        typeof(HoverTip).GetField("<Description>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? IdField =
        typeof(HoverTip).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    public bool ShouldShow(Player player)
    {
        if (player == null)
        {
            return false;
        }

        var stats = StatsTrackerManager.Instance.GetStats(player.NetId);
        return stats != null && !stats.IsEmpty;
    }

    public HoverTip? CreateHoverTip(Player player)
    {
        if (player == null)
        {
            return null;
        }

        var stats = StatsTrackerManager.Instance.GetStats(player.NetId);
        if (stats == null || stats.IsEmpty)
        {
            return null;
        }

        var title = ModLocalization.Get("stats.title", "Stats");
        var lines = stats.GetAll()
            .Select(kv => $"{ModLocalization.Get(kv.Key, kv.Key)}: {(long)kv.Value}");

        var description = string.Join("\n", lines);
        return CreateHoverTip(title, description, Id);
    }

    private static HoverTip CreateHoverTip(string title, string description, string id)
    {
        HoverTip tip = default;
        var tr = __makeref(tip);
        TitleField?.SetValueDirect(tr, title);
        DescriptionField?.SetValueDirect(tr, description);
        IdField?.SetValueDirect(tr, id);
        return tip;
    }
}
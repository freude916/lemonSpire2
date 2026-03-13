using System.Reflection;
using lemonSpire2.PlayerStateEx;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace lemonSpire2.StatsTracker;

public class StatsTooltipProvider : ITooltipProvider
{
    private static readonly FieldInfo? TitleField =
        typeof(HoverTip).GetField("<Title>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? DescriptionField =
        typeof(HoverTip).GetField("<Description>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? IdField =
        typeof(HoverTip).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    public string Id => "lemonSpire2.stats";
    public int Priority => 100;

    public bool ShouldShow(Player player)
    {
        if (player == null) return false;

        var stats = StatsTrackerManager.Instance.GetStats(player.NetId);
        return stats != null && !stats.IsEmpty;
    }

    public HoverTip? CreateHoverTip(Player player)
    {
        if (player == null) return null;

        var stats = StatsTrackerManager.Instance.GetStats(player.NetId);
        if (stats == null || stats.IsEmpty) return null;

        var title = new LocString("gameplay_ui", "LEMONSPIRE.stats.title").GetFormattedText();

        // Sorted by key to ensure consistent order, group by prefix (e.g. stats.combat, stats.total)
        var sortedStats = stats.GetAll()
            .OrderBy(kv => kv.Key)
            .ToList();

        var lines = new List<string>();
        string? currentPrefix = null;

        foreach (var kv in sortedStats)
        {
            var key = kv.Key;
            // Extract prefix
            var parts = key.Split('.');
            if (parts.Length >= 2)
            {
                var prefix = $"{parts[0]}.{parts[1]}";
                if (prefix != currentPrefix)
                {
                    currentPrefix = prefix;
                    var groupTitle = new LocString("gameplay_ui", $"LEMONSPIRE.{prefix}").GetFormattedText();
                    lines.Add($"[{groupTitle}]");
                }

                var localizedName = new LocString("gameplay_ui", $"LEMONSPIRE.{key}").GetFormattedText();
                lines.Add($"  {localizedName}: {(int)kv.Value}");
            }
            else
            {
                var localizedName = new LocString("gameplay_ui", $"LEMONSPIRE.{key}").GetFormattedText();
                lines.Add($"{localizedName}: {(int)kv.Value}");
            }
        }

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

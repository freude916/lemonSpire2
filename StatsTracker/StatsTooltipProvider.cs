using lemonSpire2.PlayerStateEx;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace lemonSpire2.StatsTracker;

public class StatsTooltipProvider : ITooltipProvider
{
    public string Id => "lemonSpire2.stats";
    public int Priority => 100;

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var stats = StatsTrackerManager.Instance.GetStats(player.NetId);
        return stats is { IsEmpty: false };
    }

    public HoverTip? CreateHoverTip(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var stats = StatsTrackerManager.Instance.GetStats(player.NetId);
        if (stats == null || stats.IsEmpty) return null;

        var title = new LocString("gameplay_ui", "LEMONSPIRE.stats.title");

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
        return new HoverTip(title, description);
    }
}

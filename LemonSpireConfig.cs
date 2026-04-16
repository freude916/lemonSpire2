using BaseLib.Config;

namespace lemonSpire2;

[HoverTipsByDefault]
internal sealed class LemonSpireConfig : SimpleModConfig
{
    [ConfigSection("FeatureFlags")] public static bool EnableQoL { get; set; } = true;

    public static bool EnableChat { get; set; } = true;

    public static bool EnableSynergyIndicator { get; set; } = true;

    public static bool EnableStatsTracker { get; set; } = true;

    public static bool EnableSync { get; set; } = true;

    public static bool EnablePlayerColor { get; set; } = true;

    [ConfigSection("PanelFlags")] public static bool GroupHandCards { get; set; } = true;
}

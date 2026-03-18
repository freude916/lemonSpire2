using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace lemonSpire2.ColorEx;

/// <summary>
///     地图绘画颜色 Patch
///     修改地图上玩家绘画线条的颜色
/// </summary>
[HarmonyPatchCategory("PlayerColor")]
[HarmonyPatch(typeof(NMapDrawings))]
public static class MapDrawColorPatch
{
    /// <summary>
    ///     在 CreateLineForPlayer 创建 Line2D 后修改颜色
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch("CreateLineForPlayer")]
    public static void CreateLineForPlayerPostfix(Player player, bool isErasing, ref Line2D __result)
    {
        ArgumentNullException.ThrowIfNull(__result);
        ArgumentNullException.ThrowIfNull(player);
        // 即使是橡皮擦模式也要修改颜色，如果不修改擦除就会失效
        // 但是即使颜色不对应也不影响，为什么呢
        // if (isErasing) return;

        // 获取自定义颜色
        var customColor = ColorManager.Instance.GetCustomColor(player.NetId);
        if (customColor.HasValue) __result.DefaultColor = customColor.Value;
    }
}

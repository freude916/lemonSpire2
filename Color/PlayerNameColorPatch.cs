using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.PlayerColor;

/// <summary>
///     玩家名字颜色 Patch
///     修改 NMultiplayerPlayerState 中玩家名字的颜色
/// </summary>
[HarmonyPatchCategory("PlayerColor")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class PlayerNameColorPatch
{
    private static readonly Dictionary<NMultiplayerPlayerState, Action<ulong, Color>> ColorChangeHandlers = new();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
    {
        var playerId = __instance.Player.NetId;

        // 创建颜色变更回调
        Action<ulong, Color> handler = (changedPlayerId, color) =>
        {
            if (changedPlayerId == playerId) UpdateNameplateColor(__instance, color);
        };

        ColorChangeHandlers[__instance] = handler;
        ColorManager.Instance.OnPlayerColorChanged += handler;

        // 设置初始颜色
        var customColor = ColorManager.Instance.GetCustomColor(playerId);
        if (customColor.HasValue) UpdateNameplateColor(__instance, customColor.Value);
    }

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NMultiplayerPlayerState __instance)
    {
        if (ColorChangeHandlers.Remove(__instance, out var handler))
            ColorManager.Instance.OnPlayerColorChanged -= handler;
    }

    private static void UpdateNameplateColor(NMultiplayerPlayerState instance, Color color)
    {
        // 使用反射获取 _nameplateLabel 字段
        var field = typeof(NMultiplayerPlayerState).GetField("_nameplateLabel",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var label = field?.GetValue(instance) as MegaLabel;
        label?.AddThemeColorOverride("font_color", color);
    }
}

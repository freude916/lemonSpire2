using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.PlayerColor;

/// <summary>
///     远程鼠标颜色 Patch
///     修改远程玩家鼠标的颜色
/// </summary>
[HarmonyPatchCategory("PlayerColor")]
[HarmonyPatch(typeof(NRemoteMouseCursor))]
public static class RemoteCursorColorPatch
{
    private static readonly Dictionary<NRemoteMouseCursor, Action<ulong, Color>> ColorChangeHandlers = new();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NRemoteMouseCursor __instance)
    {
        var playerId = __instance.PlayerId;

        // 创建颜色变更回调
        Action<ulong, Color> handler = (changedPlayerId, color) =>
        {
            if (changedPlayerId == playerId) UpdateCursorColor(__instance, color);
        };

        ColorChangeHandlers[__instance] = handler;
        ColorManager.Instance.OnPlayerColorChanged += handler;

        // 设置初始颜色
        var customColor = ColorManager.Instance.GetCustomColor(playerId);
        if (customColor.HasValue) UpdateCursorColor(__instance, customColor.Value);
    }

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NRemoteMouseCursor __instance)
    {
        if (ColorChangeHandlers.Remove(__instance, out var handler))
            ColorManager.Instance.OnPlayerColorChanged -= handler;
    }

    private static void UpdateCursorColor(NRemoteMouseCursor instance, Color color)
    {
        // 获取 TextureRect 子节点并设置 Modulate
        var textureRect = instance.GetNode<TextureRect>("TextureRect");
        if (textureRect != null) textureRect.Modulate = color;
    }
}

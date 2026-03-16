using System.Reflection;
using Godot;
using HarmonyLib;
using lemonSpire2.QoL;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;

namespace lemonSpire2.PlayerStateEx;

/// <summary>
///     Patch 为 NMultiplayerPlayerExpandedState 中的卡牌添加 HoverTip <br/>
///     <li> 游戏原生的 ExpandPlayerState 没有为 NDeckHistoryEntry 提供悬浮提示 </li>
/// </summary>
[HarmonyPatch(typeof(NMultiplayerPlayerExpandedState))]
public static class NMultiplayerPlayerExpandedStatePatch
{
    // 私有方法通过字符串名称调用，不能用 nameof()
    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerExpandedState __instance)
    {
        // 使用反射获取私有字段 _cardContainer
        var cardContainerField = typeof(NMultiplayerPlayerExpandedState)
            .GetField("_cardContainer", BindingFlags.NonPublic | BindingFlags.Instance);
        var cardContainer = cardContainerField?.GetValue(__instance) as Control;
        if (cardContainer == null) return;

        // 为每个 NDeckHistoryEntry 绑定 HoverTip
        foreach (var child in cardContainer.GetChildren())
            if (child is NDeckHistoryEntry entry)
            {
                var card = entry.Card;
                CardHoverTipHelper.BindCardHoverTip(entry, () => card, HoverTipAlignment.Right);
            }
    }
}

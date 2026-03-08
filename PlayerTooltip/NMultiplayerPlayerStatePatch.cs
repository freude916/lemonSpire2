using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.PlayerTooltip;

/// <summary>
/// Patch for NMultiplayerPlayerState to show tooltips from registered providers.
/// This patch is decoupled from specific tooltip content - providers register themselves.
/// </summary>
[HarmonyPatchCategory("PlayerTooltip")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class NMultiplayerPlayerStatePatch
{
    private static readonly FieldInfo? NetworkProblemIndicatorField =
        typeof(NMultiplayerPlayerState).GetField("_networkProblemIndicator",
            BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPostfix]
    [HarmonyPatch("UpdateHighlightedState")]
    public static void UpdateHighlightedStatePostfix(NMultiplayerPlayerState __instance, bool ____isHighlighted)
    {
        if (!____isHighlighted)
        {
            return;
        }

        var indicator = NetworkProblemIndicatorField?.GetValue(__instance) as NMultiplayerNetworkProblemIndicator;
        if (indicator != null && indicator.IsShown)
        {
            return;
        }

        ShowTooltips(__instance, __instance.Player);
    }

    private static void ShowTooltips(NMultiplayerPlayerState instance, Player player)
    {
        if (!PlayerTooltipRegistry.HasProviders)
        {
            return;
        }

        var hoverTips = PlayerTooltipRegistry.GetHoverTips(player);

        var tipSet = NHoverTipSet.CreateAndShow(instance, hoverTips);
        tipSet.GlobalPosition = instance.GlobalPosition + Vector2.Down * 80f;
    }
}
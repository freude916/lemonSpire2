using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.DamageTracker;

[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class NMultiplayerPlayerStatePatch
{
    #region Reflection Fields

    private static readonly FieldInfo? NetworkProblemIndicatorField =
        typeof(NMultiplayerPlayerState).GetField("_networkProblemIndicator", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? HoverTipTitleField =
        typeof(HoverTip).GetField("<Title>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? HoverTipDescriptionField =
        typeof(HoverTip).GetField("<Description>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo? HoverTipIdField =
        typeof(HoverTip).GetField("<Id>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

    #endregion

    [HarmonyPostfix]
    [HarmonyPatch("UpdateHighlightedState")]
    public static void UpdateHighlightedStatePostfix(NMultiplayerPlayerState __instance, bool ____isHighlighted)
    {
        if (!____isHighlighted) return;

        var indicator = NetworkProblemIndicatorField?.GetValue(__instance) as NMultiplayerNetworkProblemIndicator;
        if (indicator != null && indicator.IsShown) return;

        ShowDamageTooltip(__instance, __instance.Player);
    }

    private static void ShowDamageTooltip(NMultiplayerPlayerState instance, Player player)
    {
        ulong netId = player.NetId;
        int combatDamage = DamageTrackerManager.Instance.GetCombatDamage(netId);
        long totalDamage = DamageTrackerManager.Instance.GetTotalDamage(netId);

        string title = ModLocalization.Get("damage.title", "Damage Stats");
        string desc = $"{ModLocalization.Get("damage.combat", "Combat Damage")}: {combatDamage}\n" +
                      $"{ModLocalization.Get("damage.total", "Total Damage")}: {totalDamage}";

        HoverTip tip = CreateHoverTip(title, desc, "lemonSpire2.damage.tooltip");
        var tipSet = NHoverTipSet.CreateAndShow(instance, tip);
        tipSet.GlobalPosition = instance.GlobalPosition + Vector2.Down * 80f;
    }

    private static HoverTip CreateHoverTip(string title, string description, string id)
    {
        HoverTip tip = default;
        TypedReference tr = __makeref(tip);
        HoverTipTitleField?.SetValueDirect(tr, title);
        HoverTipDescriptionField?.SetValueDirect(tr, description);
        HoverTipIdField?.SetValueDirect(tr, id);
        return tip;
    }
}
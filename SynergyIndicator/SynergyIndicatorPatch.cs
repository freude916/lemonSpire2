using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace lemonSpire2.SynergyIndicator;

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class SynergyIndicatorPatch
{
    private const string HandshakeEmoji = "🤝";

    private static readonly FieldInfo? TopContainerField =
        typeof(NMultiplayerPlayerState).GetField("_topContainer", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly Dictionary<NMultiplayerPlayerState, Label> _indicators = new();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
        => CreateIndicator(__instance);

    [HarmonyPostfix]
    [HarmonyPatch("OnCombatSetUp")]
    public static void OnCombatSetUpPostfix(NMultiplayerPlayerState __instance)
        => UpdateIndicator(__instance);

    [HarmonyPostfix]
    [HarmonyPatch("OnCardAdded")]
    public static void OnCardAddedPostfix(NMultiplayerPlayerState __instance)
        => UpdateIndicator(__instance);

    [HarmonyPostfix]
    [HarmonyPatch("OnCardRemoved")]
    public static void OnCardRemovedPostfix(NMultiplayerPlayerState __instance)
        => UpdateIndicator(__instance);

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NMultiplayerPlayerState __instance)
        => RemoveIndicator(__instance);

    private static void CreateIndicator(NMultiplayerPlayerState instance)
    {
        if (_indicators.ContainsKey(instance))
        {
            return;
        }

        var topContainer = TopContainerField?.GetValue(instance) as HBoxContainer;
        if (topContainer == null)
        {
            return;
        }

#pragma warning disable CA2000 // Label 所有权转移到场景树，由 RemoveIndicator 管理
        var label = new Label
        {
            Name = "HandshakeIndicator",
            Text = HandshakeEmoji,
            Visible = false
        };
#pragma warning restore CA2000
        label.AddThemeFontSizeOverride("font_size", 24);

        topContainer.AddChild(label);

        _indicators[instance] = label;
    }

    private static void RemoveIndicator(NMultiplayerPlayerState instance)
    {
        if (_indicators.Remove(instance, out var label) && GodotObject.IsInstanceValid(instance))
        {
            label.QueueFree();
        }
    }

    private static void UpdateIndicator(NMultiplayerPlayerState instance)
    {
        if (!_indicators.TryGetValue(instance, out var label))
        {
            return;
        }

        var cards = instance.Player?.PlayerCombatState?.Hand?.Cards;
        if (cards == null)
        {
            label.Visible = false;
            return;
        }

        label.Visible = cards.Any(c => c.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly);
    }
}
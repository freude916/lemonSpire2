using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace lemonSpire2.SynergyIndicator;

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class SynergyIndicatorPatch
{
    private const string HandshakeEmoji = "🤝";

    private static readonly FieldInfo? TopContainerField =
        typeof(NMultiplayerPlayerState).GetField("_topContainer", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly Dictionary<NMultiplayerPlayerState, Label> _indicators = new();
    private static readonly Dictionary<NMultiplayerPlayerState, bool> _indicatorVisibility = new();

    private static AudioStream? _noticeSound;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
    {
        _noticeSound ??= GD.Load<AudioStream>("res://lemonSpire2/synergy-notice.mp3");
        CreateIndicator(__instance);
        CombatManager.Instance.TurnStarted += OnTurnStarted;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnCombatSetUp")]
    public static void OnCombatSetUpPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        UpdateIndicator(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnCardAdded")]
    public static void OnCardAddedPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        UpdateIndicator(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnCardRemoved")]
    public static void OnCardRemovedPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        UpdateIndicator(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NMultiplayerPlayerState __instance)
    {
        CombatManager.Instance.TurnStarted -= OnTurnStarted;
        RemoveIndicator(__instance);
    }

    private static void OnTurnStarted(CombatState _)
    {
        // Reset visibility tracking at turn start so newly drawn synergy cards trigger flash+sound
        foreach (var kvp in _indicators.ToList())
        {
            _indicatorVisibility[kvp.Key] = false;
        }
    }

    private static void FlashIndicator(Label label)
    {
        // Use tween to create flashing effect
        var tween = label.CreateTween();
        tween.SetLoops(3);
        
        tween.TweenProperty(label, "scale", Vector2.One * 2f, 0.3)
            .SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(label, "modulate", Colors.Yellow, 0.2);
        
        tween.TweenProperty(label, "scale", Vector2.One, 0.3)
            .SetEase(Tween.EaseType.In);
        tween.Parallel().TweenProperty(label, "modulate", Colors.White, 0.2);
    }

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

#pragma warning disable CA2000 
        //! Label Ownership is transferred to the scene tree, managed by RemoveIndicator
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
        _indicatorVisibility[instance] = false;
    }

    private static void RemoveIndicator(NMultiplayerPlayerState instance)
    {
#pragma warning disable CA2000 
        //! Object acquired from _indicators will be managed by this method, so we are responsible for freeing it.
        if (_indicators.Remove(instance, out var label) && GodotObject.IsInstanceValid(label))
#pragma warning restore CA2000
        {
            label.QueueFree();
        }
        _indicatorVisibility.Remove(instance);
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
            _indicatorVisibility[instance] = false;
            return;
        }

        var shouldShow = cards.Any(c => c.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly);
        var wasVisible = _indicatorVisibility.GetValueOrDefault(instance, false);
        
        // Flash and play sound when the indicator appears
        if (shouldShow && !wasVisible)
        {
            FlashIndicator(label);
            PlayNoticeSound(instance);
        }

        label.Visible = shouldShow;
        _indicatorVisibility[instance] = shouldShow;
    }

    private static void PlayNoticeSound(Node node)
    {
        if (_noticeSound == null)
        {
            return;
        }

#pragma warning disable CA2000 
        //! AudioStreamPlayer's ownership is transferred to the scene tree, and it will be freed by QueueFree after playback is finished.
        var player = new AudioStreamPlayer
        {
            Stream = _noticeSound,
            VolumeDb = -3f
        };
#pragma warning restore CA2000
        node.AddChild(player);
        player.Play();
        player.Finished += player.QueueFree;
    }
}
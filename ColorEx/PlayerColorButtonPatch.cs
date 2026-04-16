using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace lemonSpire2.ColorEx;

/// <summary>
///     玩家颜色选择按钮 Patch
///     在本地玩家的状态栏添加一个颜色选择按钮
/// </summary>
[HarmonyPatchCategory("PlayerColor")]
[HarmonyPatch(typeof(NMultiplayerPlayerState))]
public static class PlayerColorButtonPatch
{
    private const string ColorPickerEmoji = "🎨";

    private static readonly FieldInfo? TopContainerField =
        typeof(NMultiplayerPlayerState).GetField("_topContainer", BindingFlags.NonPublic | BindingFlags.Instance);

    // 存储按钮引用
    private static WeakReference<Button>? _colorButton;

    // 是否已订阅颜色变更事件
    private static bool _subscribedToColorChange;
    private static bool _subscribedToCombatEvents;

    public static string Title => new LocString("gameplay_ui", "LEMONSPIRE.color_picker.title").GetFormattedText();
    public static string Tooltip => new LocString("gameplay_ui", "LEMONSPIRE.color_picker.tooltip").GetFormattedText();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        // 只对本地玩家显示颜色按钮
        if (!LocalContext.IsMe(__instance.Player)) return;

        if (TopContainerField?.GetValue(__instance) is not HBoxContainer topContainer)
        {
            ColorManager.Log.Info("Failed to get TopContainer from NMultiplayerPlayerState");
            return;
        }

        var playerId = __instance.Player.NetId;

        // 创建颜色选择按钮
        var colorButton = CreateColorButton(playerId);
        topContainer.AddChild(colorButton);
        colorButton.MoveToFront();

        // 确保 topContainer 在最前面
        var topContainerParent = topContainer.GetParent();
        topContainerParent?.MoveChild(topContainer, topContainerParent.GetChildCount() - 1);

        // 注册按钮引用
        _colorButton = new WeakReference<Button>(colorButton);
        EnsureCombatEventSubscribed();
        UpdateButtonVisibility();
        colorButton.Visible = false; // 默认隐藏呗，大不了下场战斗才能打开

        // 只订阅一次颜色变更事件
        if (!_subscribedToColorChange)
        {
            ColorManager.Instance.OnPlayerColorChanged += OnPlayerColorChanged;
            _subscribedToColorChange = true;
        }

        ColorManager.Log.Debug($"ColorButton added for player {playerId}");
    }

    [HarmonyPrefix]
    [HarmonyPatch("_ExitTree")]
    public static void ExitTreePrefix(NMultiplayerPlayerState __instance)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        if (!LocalContext.IsMe(__instance.Player)) return;
        _colorButton = null;
    }

    private static Button CreateColorButton(ulong playerId)
    {
        var button = new Button
        {
            Text = ColorPickerEmoji,
            CustomMinimumSize = new Vector2(32, 32),
            TooltipText = Tooltip,
            ZIndex = 100
        };

        button.AddThemeFontSizeOverride("font_size", 20);

        // 设置背景色为当前颜色
        var currentColor = ColorManager.Instance.GetCustomColor(playerId) ?? Colors.White;
        UpdateButtonStyle(button, currentColor);

        // 点击时弹出颜色选择器
        button.Pressed += () => ShowColorPicker(playerId, button);

        return button;
    }

    private static void UpdateButtonStyle(Button button, Color color)
    {
        var style = new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = new Color(0.8f, 0.8f, 0.8f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
    }

    private static void ShowColorPicker(ulong playerId, Button button)
    {
        var currentColor = ColorManager.Instance.GetCustomColor(playerId) ?? Colors.White;

        // 创建弹窗颜色选择器
        var popup = new PopupPanel
        {
            Title = Title,
            Borderless = false
        };

        var colorPicker = new ColorPicker
        {
            Color = currentColor,
            EditAlpha = false
        };

        // 不在拖动时更新，只在关闭时应用
        popup.AddChild(colorPicker);
        button.GetTree().Root.AddChild(popup);

        // 弹出在按钮旁边
        popup.PopupOnParent(new Rect2I(
            (int)(button.GlobalPosition.X + button.Size.X),
            (int)button.GlobalPosition.Y,
            300,
            300
        ));

        // 关闭时应用最终颜色
        popup.PopupHide += SubmitColor;

        return;

        void SubmitColor()
        {
            var finalColor = colorPicker.Color;
            OnColorChanged(playerId, finalColor, button);
            popup.QueueFree();
        }
    }

    private static void OnColorChanged(ulong playerId, Color color, Button button)
    {
        // 设置本地颜色
        ColorManager.Instance.SetPlayerColor(playerId, color);

        // 广播给其他玩家
        ColorNetworkPatch.NetworkHandler?.BroadcastColorChange(playerId, color);

        // 更新按钮样式
        UpdateButtonStyle(button, color);

        ColorManager.Log.Info($"Player {playerId} changed color to {color}");
    }

    private static void OnPlayerColorChanged(ulong playerId, Color color)
    {
        // 更新按钮显示
        if (_colorButton == null || !_colorButton.TryGetTarget(out var button) ||
            !GodotObject.IsInstanceValid(button)) return;
        UpdateButtonStyle(button, color);
        UpdateButtonVisibility();
    }

    private static void EnsureCombatEventSubscribed()
    {
        if (_subscribedToCombatEvents) return;

        CombatManager.Instance.CombatSetUp += OnCombatSetUp;
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        _subscribedToCombatEvents = true;
    }

    private static void OnCombatSetUp(CombatState _)
    {
        UpdateButtonVisibility();
    }

    private static void OnCombatEnded(CombatRoom _)
    {
        UpdateButtonVisibility();
    }

    private static void UpdateButtonVisibility()
    {
        Button? button = null;
        _colorButton?.TryGetTarget(out button);
        if (button == null) return;
        button.Visible = !CombatManager.Instance.IsInProgress;
    }
}

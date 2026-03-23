using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

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
    
    public static string Title => new LocString("gameplay_ui", "LEMONSPIRE.color_picker.title").GetFormattedText();
    public static string Tooltip => new LocString("gameplay_ui", "LEMONSPIRE.color_picker.tooltip").GetFormattedText();

    // 存储按钮引用，用于更新颜色显示
    private static readonly Dictionary<ulong, WeakReference<Button>> ColorButtons = new();

    // 是否已订阅颜色变更事件
    private static bool _subscribedToColorChange;

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NMultiplayerPlayerState __instance)
    {
        // 只对本地玩家显示颜色按钮
        if (!LocalContext.IsMe(__instance.Player)) return;

        var topContainer = TopContainerField?.GetValue(__instance) as HBoxContainer;
        if (topContainer == null)
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
        if (topContainerParent != null)
            topContainerParent.MoveChild(topContainer, topContainerParent.GetChildCount() - 1);

        // 注册按钮引用
        ColorButtons[playerId] = new WeakReference<Button>(colorButton);

        // 只订阅一次颜色变更事件
        if (!_subscribedToColorChange)
        {
            ColorManager.Instance.OnPlayerColorChanged += OnPlayerColorChanged;
            _subscribedToColorChange = true;
        }

        ColorManager.Log.Debug($"ColorButton added for player {playerId}");
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
        popup.CloseRequested += () =>
        {
            var finalColor = colorPicker.Color;
            OnColorChanged(playerId, finalColor, button);
            popup.QueueFree();
        };
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
        if (ColorButtons.TryGetValue(playerId, out var weakRef) &&
            weakRef.TryGetTarget(out var button) &&
            GodotObject.IsInstanceValid(button))
            UpdateButtonStyle(button, color);
    }
}

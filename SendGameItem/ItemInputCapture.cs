using System.Reflection;
using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SendGameItem;

/// <summary>
///     全局输入捕获节点 — 拦截 Alt+Click 发送物品链接
///     需要添加到场景树中才能工作
/// </summary>
public partial class ItemInputCapture : Control
{
    private static readonly FieldInfo? HoverTipOwnerField =
        typeof(NHoverTipSet).GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);

    private static Logger Log => SendItemInputPatch.Log;

    /// <summary>
    ///     调试用：当 Alt+Click 找不到物品时，是否阻止事件传播
    ///     设为 true 可以防止 Alt+Click 在商店等场景触发购买
    ///     设为 false 允许其他 Mod 处理 Alt+Click
    /// </summary>
    public static bool BlockAltClickOnNoItem { get; set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        Log.Info("ItemInputCapture ready");
    }

    public override void _Input(InputEvent @event)
    {
        // MiddleClick: 把当前捕获的物品插入聊天输入框，交给输入解析链继续处理。
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Middle })
        {
            HandleMiddleClick();
            return;
        }

        // Alt+LeftClick: 从悬停的节点发送物品
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left, AltPressed: true })
        {
            HandleAltLeftClick();
            return;
        }

        // Alt+RightClick: 从当前显示的 HoverTip 发送
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right, AltPressed: true })
        {
            HandleAltRightClick();
            return;
        }
    }

    private void HandleMiddleClick()
    {
        Log.Debug("MiddleClick detected");

        var hovered = GetHoveredControl();
        if (hovered == null)
        {
            Log.Debug("No hovered control");
            return;
        }

        if (IsInsideBlockingControl(hovered))
        {
            Log.Debug("Inside blocking control, ignoring");
            return;
        }

        var segment = ItemInputHandler.FindItemToTooltipSegment(hovered);
        if (!ItemInputInsertFormatter.TryFormat(segment, out var insertionText))
        {
            Log.Debug("Hovered item is not insertable");
            return;
        }

        if (!ChatStore.TryInsertIntoInput(insertionText))
            return;

        Log.Info($"Inserted item reference into chat input: {insertionText}");
        GetViewport()?.SetInputAsHandled();
    }

    private void HandleAltLeftClick()
    {
        Log.Debug("Alt+LeftClick detected");

        var hovered = GetHoveredControl();
        if (hovered == null)
        {
            Log.Debug("No hovered control");
            if (BlockAltClickOnNoItem)
                GetViewport()?.SetInputAsHandled();
            return;
        }

        Log.Debug($"Hovered: {hovered.Name} ({hovered.GetType().Name})");

        if (IsInsideBlockingControl(hovered))
        {
            Log.Debug("Inside blocking control, ignoring");
            return;
        }

        var segment = ItemInputHandler.FindItemToTooltipSegment(hovered);
        if (segment == null)
        {
            Log.Debug("No item segment found");
            if (BlockAltClickOnNoItem)
                GetViewport()?.SetInputAsHandled();
            return;
        }

        Log.Info($"Found item segment: {segment.Render()}");
        ChatStore.SendToChat(segment);
        GetViewport()?.SetInputAsHandled();
    }

    private void HandleAltRightClick()
    {
        Log.Debug("Alt+RightClick detected");

        var hovered = GetHoveredControl();
        if (hovered != null)
        {
            Log.Debug($"Hovered: {hovered.Name} ({hovered.GetType().Name})");

            if (IsInsideBlockingControl(hovered))
            {
                Log.Debug("Inside blocking control, ignoring");
                return;
            }

            var hoveredSegments = ItemInputHandler.FindItemAndHoverTipSegments(hovered).ToArray();
            if (hoveredSegments.Length > 0)
            {
                Log.Info($"Found {hoveredSegments.Length} hovered item segments");
                ChatStore.SendToChat([.. hoveredSegments]);
                GetViewport()?.SetInputAsHandled();
                return;
            }
        }

        Log.Debug("Hovered item not resolved, trying visible HoverTip fallback");

        var container = NGame.Instance?.HoverTipsContainer;
        if (container == null)
        {
            Log.Debug("No HoverTipsContainer found");
            return;
        }

        foreach (var child in container.GetChildren())
        {
            if (child is not NHoverTipSet { Visible: true } tipSet)
                continue;

            var segments = ExtractSegmentsFromHoverTipSet(tipSet).ToArray();
            if (segments.Length == 0) continue;
            Log.Info($"Captured {segments.Length} segments from HoverTip");
            ChatStore.SendToChat([.. segments]);
            GetViewport()?.SetInputAsHandled();
            return;
        }
    }

    private static IReadOnlyList<IMsgSegment> ExtractSegmentsFromHoverTipSet(NHoverTipSet tipSet)
    {
        var ownerSegments = ExtractFromOwner(tipSet).ToArray();
        if (ownerSegments.Length > 0)
        {
            Log.Info($"Extracted owner segments: {string.Join(", ", ownerSegments.Select(s => s.Render()))}");
            return ownerSegments;
        }

        var cardSegments = ExtractFromCardContainer(tipSet);
        if (cardSegments.Count > 0)
        {
            var textSegments = ExtractFromTextContainer(tipSet);
            cardSegments.AddRange(textSegments);
            Log.Info($"Extracted card fallback segments: {string.Join(", ", cardSegments.Select(s => s.Render()))}");
            return cardSegments;
        }

        var textFallbackSegments = ExtractFromTextContainer(tipSet).ToArray();
        return textFallbackSegments.Length > 0 ? textFallbackSegments : [];
    }

    private static IReadOnlyList<IMsgSegment> ExtractFromOwner(NHoverTipSet tipSet)
    {
        return HoverTipOwnerField?.GetValue(tipSet) is not Node owner
            ? []
            : ItemInputHandler.FindItemAndHoverTipSegments(owner);
    }

    private Control? GetHoveredControl()
    {
        return GetViewport()?.GuiGetHoveredControl();
    }

    private static List<TooltipSegment> ExtractFromCardContainer(NHoverTipSet tipSet)
    {
        var cardContainer = tipSet.GetNodeOrNull<NHoverTipCardContainer>("cardHoverTipContainer");
        if (cardContainer == null || cardContainer.GetChildCount() <= 0)
            return [];

        var segments = new List<TooltipSegment>();

        foreach (var cardTipNode in cardContainer.GetChildren())
        {
            var nCard = cardTipNode.GetNodeOrNull<NCard>("%Card");
            if (nCard?.Model == null) continue;

            segments.Add(new TooltipSegment
            {
                Tooltip = CardTooltip.FromModel(nCard.Model)
            });
        }

        return segments;
    }

    private static List<TooltipSegment> ExtractFromTextContainer(NHoverTipSet tipSet)
    {
        var textContainer = tipSet.GetNodeOrNull<VFlowContainer>("textHoverTipContainer");
        if (textContainer == null || textContainer.GetChildCount() <= 0)
            return [];

        // 收集所有文本 tooltip 的内容
        var segments = new List<TooltipSegment>();

        foreach (var child in textContainer.GetChildren())
        {
            if (child is not Control tipControl) continue;

            var titleLabel = tipControl.GetNodeOrNull<Label>("%Title");
            var descLabel = tipControl.GetNodeOrNull<RichTextLabel>("%Description");
            var iconRect = tipControl.GetNodeOrNull<TextureRect>("%Icon");

            var title = titleLabel?.Text;
            var description = descLabel?.Text ?? "";
            var iconPath = iconRect?.Texture?.ResourcePath;

            // 检查是否是 debuff（通过背景材质判断）
            var isDebuff = false;
            var bg = tipControl.GetNodeOrNull<CanvasItem>("%Bg");
            if (bg?.Material != null)
                // debuff tooltip 使用特定材质
                isDebuff = bg.Material.ResourcePath?.Contains("debuff", StringComparison.OrdinalIgnoreCase) == true;

            if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(description))
                segments.Add(new TooltipSegment
                {
                    Tooltip = new RichTextTooltip
                    {
                        Title = string.IsNullOrEmpty(title) ? null : title,
                        Description = description,
                        IsDebuff = isDebuff,
                        IconPath = iconPath
                    }
                });
        }

        return segments;
    }

    #region Alt+Click Bypass

    /// <summary>
    ///     已注册的阻塞控件列表 — 这些控件内部的 Alt+Click 将被放过
    /// </summary>
    private static readonly WeakNodeRegistry<Control> BlockingControls = new();

    /// <summary>
    ///     UI 组件调用此方法注册自己，InputCapture 将放过其内部的 Alt+Click
    /// </summary>
    public static void RegisterBlockingControl(Control control)
    {
        BlockingControls.Register(control);
    }

    /// <summary>
    ///     检查是否在任何已注册的阻塞控件内
    /// </summary>
    public static bool IsInsideBlockingControl(Control? control)
    {
        if (control == null) return false;

        var found = false;
        BlockingControls.ForEachLive(c =>
        {
            if (c == control || c.IsAncestorOf(control))
                found = true;
        });

        return found;
    }

    #endregion
}

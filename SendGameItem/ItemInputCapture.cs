using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Intent;
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
    /// <summary>
    ///     已注册的阻塞控件列表 — 这些控件内部的 Alt+Click 将被放过
    /// </summary>
    private static readonly WeakNodeRegistry<Control> BlockingControls = new();

    private static Logger Log => SendItemInputPatch.Log;

    /// <summary>
    ///     调试用：当 Alt+Click 找不到物品时，是否阻止事件传播
    ///     设为 true 可以防止 Alt+Click 在商店等场景触发购买
    ///     设为 false 允许其他 Mod 处理 Alt+Click
    /// </summary>
    public static bool BlockAltClickOnNoItem { get; set; }

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

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        Log.Info("ItemInputCapture ready");
    }

    public override void _Input(InputEvent @event)
    {
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

    private void HandleAltLeftClick()
    {
        Log.Debug("Alt+LeftClick detected");

        var hovered = GetViewport()?.GuiGetHoveredControl();
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

        Log.Info($"Found item: {segment.Tooltip.Render()}");
        SendItemSegment(segment);
        GetViewport()?.SetInputAsHandled();
    }

    private void HandleAltRightClick()
    {
        Log.Debug("Alt+RightClick detected - trying to capture visible HoverTip");

        var container = NGame.Instance?.HoverTipsContainer;
        if (container == null)
        {
            Log.Debug("No HoverTipsContainer found");
            return;
        }

        foreach (var child in container.GetChildren())
        {
            if (child is not NHoverTipSet tipSet || !tipSet.Visible)
                continue;

            var segment = ExtractSegmentFromHoverTipSet(tipSet);
            if (segment != null)
            {
                Log.Info($"Captured from HoverTip: {segment.Tooltip.Render()}");
                SendItemSegment(segment);
                GetViewport()?.SetInputAsHandled();
                return;
            }
        }

        Log.Debug("No visible HoverTip with sendable content");
    }

    private static TooltipSegment? ExtractSegmentFromHoverTipSet(NHoverTipSet tipSet)
    {
        // 1. 尝试从 cardHoverTipContainer 获取卡牌（精确）
        var cardSegment = ExtractFromCardContainer(tipSet);
        if (cardSegment != null) return cardSegment;

        // 2. 从 textHoverTipContainer 提取文本内容
        var textSegment = ExtractFromTextContainer(tipSet);
        if (textSegment != null) return textSegment;

        return null;
    }

    private static TooltipSegment? ExtractFromCardContainer(NHoverTipSet tipSet)
    {
        var cardContainer = tipSet.GetNodeOrNull<NHoverTipCardContainer>("cardHoverTipContainer");
        if (cardContainer == null || cardContainer.GetChildCount() <= 0)
            return null;

        foreach (var cardTipNode in cardContainer.GetChildren())
        {
            var nCard = cardTipNode.GetNodeOrNull<NCard>("%Card");
            if (nCard?.Model == null) continue;

            return new TooltipSegment
            {
                Tooltip = CardTooltip.FromModel(nCard.Model)
            };
        }

        return null;
    }

    private static TooltipSegment? ExtractFromTextContainer(NHoverTipSet tipSet)
    {
        var textContainer = tipSet.GetNodeOrNull<VFlowContainer>("textHoverTipContainer");
        if (textContainer == null || textContainer.GetChildCount() <= 0)
            return null;

        // 收集所有文本 tooltip 的内容
        var tips = new List<(string? Title, string Description, bool IsDebuff, string? IconPath)>();

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
                tips.Add((string.IsNullOrEmpty(title) ? null : title, description, isDebuff, iconPath));
        }

        if (tips.Count == 0) return null;

        // 如果只有一个 tooltip，直接发送
        {
            var (title, desc, isDebuff, iconPath) = tips[0];
            return new TooltipSegment
            {
                Tooltip = new RichTextTooltip
                {
                    Title = title,
                    Description = desc,
                    IsDebuff = isDebuff,
                    IconPath = iconPath
                }
            };
        }

        // TODO: 如果有多个 tooltip，需要重构方案来正确发送，目前所有都只能 Send 一个 Segment，无法表达多个 tooltip 的情况
    }

    private static void SendItemSegment(TooltipSegment segment)
    {
        var store = ChatStore.Instance;
        if (store == null)
        {
            Log.Warn("ChatStore.Instance is null");
            return;
        }

        store.Dispatch(new IntentSendSegments
        {
            ReceiverId = 0,
            Segments = [segment]
        });
    }
}

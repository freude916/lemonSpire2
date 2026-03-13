using Godot;
using lemonSpire2.Chat;
using lemonSpire2.Chat.Intent;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace lemonSpire2.SendItem;

/// <summary>
///     全局输入捕获节点 — 拦截 Alt+Click 发送物品链接
///     需要添加到场景树中才能工作
/// </summary>
public partial class ItemInputCapture : Control
{
    /// <summary>
    ///     调试用：当 Alt+Click 找不到物品时，是否阻止事件传播
    ///     设为 true 可以防止 Alt+Click 在商店等场景触发购买
    ///     设为 false 允许其他 Mod 处理 Alt+Click
    /// </summary>
    public static bool BlockAltClickOnNoItem { get; set; } = false;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        MainFile.Logger.Info("ItemInputCapture ready");
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
        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: MouseButton.Right, AltPressed: true
            }) HandleAltRightClick();
    }

    private void HandleAltLeftClick()
    {
        MainFile.Logger.Debug("Alt+LeftClick detected");

        var hovered = GetViewport()?.GuiGetHoveredControl();
        if (hovered == null)
        {
            MainFile.Logger.Debug("No hovered control");
            if (BlockAltClickOnNoItem)
                GetViewport()?.SetInputAsHandled();
            return;
        }

        MainFile.Logger.Debug($"Hovered: {hovered.Name} ({hovered.GetType().Name})");

        if (IsInsideChatPanel(hovered))
        {
            MainFile.Logger.Debug("Inside chat panel, ignoring");
            return;
        }

        var segment = ItemInputHandler.FindItemToTooltipSegment(hovered);
        if (segment == null)
        {
            MainFile.Logger.Debug("No item segment found");
            if (BlockAltClickOnNoItem)
                GetViewport()?.SetInputAsHandled();
            return;
        }

        MainFile.Logger.Info($"Found item: {segment.DisplayName}");
        SendItemSegment(segment);
        GetViewport()?.SetInputAsHandled();
    }

    private void HandleAltRightClick()
    {
        MainFile.Logger.Debug("Alt+RightClick detected - trying to capture visible HoverTip");

        var container = NGame.Instance?.HoverTipsContainer;
        if (container == null)
        {
            MainFile.Logger.Debug("No HoverTipsContainer found");
            return;
        }

        foreach (var child in container.GetChildren())
        {
            if (child is not NHoverTipSet tipSet || !tipSet.Visible)
                continue;

            var segment = ExtractSegmentFromHoverTipSet(tipSet);
            if (segment != null)
            {
                MainFile.Logger.Info($"Captured from HoverTip: {segment.DisplayName}");
                SendItemSegment(segment);
                GetViewport()?.SetInputAsHandled();
                return;
            }
        }

        MainFile.Logger.Debug("No visible HoverTip with sendable content");
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
                Tooltip = CardTooltip.FromModel(nCard.Model),
                DisplayName = nCard.Model.Title
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
                },
                DisplayName = title ?? "Tooltip"
            };
        }

        // TODO: 如果有多个 tooltip，需要重构方案来正确发送，目前所有都只能 Send 一个 Segment，无法表达多个 tooltip 的情况
    }

    private static void SendItemSegment(TooltipSegment segment)
    {
        var store = ChatStore.Instance;
        if (store == null)
        {
            MainFile.Logger.Warn("ChatStore.Instance is null");
            return;
        }


        store.Dispatch(new IntentSendSegments
        {
            receiverId = 0,
            Segments = [segment]
        });
    }

    private static bool IsInsideChatPanel(Control? control)
    {
        if (control == null)
            return false;

        var found = false;
        ChatUiPatch.ChatUIs.ForEachLive(chatUI =>
        {
            if (chatUI == control || chatUI.IsAncestorOf(control))
                found = true;
        });

        return found;
    }
}

using Godot;
using lemonSpire2.Chat.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace lemonSpire2.SendItem;

/// <summary>
///     物品输入处理器 — 检测 Alt+点击物品并发送消息
/// </summary>
public static class ItemInputHandler
{
    /// <summary>
    ///     尝试处理 Alt+左键点击，发送物品链接
    /// </summary>
    /// <param name="evt">输入事件</param>
    /// <param name="excludeRoot">排除的根节点（如 ChatPanel），避免点击自身时触发</param>
    /// <param name="onItemDetected">检测到物品时的回调</param>
    /// <returns>是否处理了该事件</returns>
    public static bool TryHandleAltClick(
        InputEvent evt,
        Control? excludeRoot,
        Action<ItemSegment, string> onItemDetected)
    {
        if (evt is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left, AltPressed: true })
        {
            return false;
        }

        var viewport = excludeRoot?.GetViewport();
        if (viewport == null)
        {
            return false;
        }

        var hovered = viewport.GuiGetHoveredControl();
        if (hovered == null)
        {
            return false;
        }

        // 排除 ChatPanel 自身
        if (excludeRoot != null && (excludeRoot.IsAncestorOf(hovered) || hovered == excludeRoot))
        {
            return false;
        }

        // 向上遍历查找物品节点
        var segment = FindItemSegment(hovered);
        if (segment == null)
        {
            return false;
        }

        // 获取提示动词
        var label = ItemLink.GetItemTypeLabel(segment.LinkType);

        // 回调
        onItemDetected(segment, label);
        return true;
    }

    /// <summary>
    ///     从节点树中查找物品并创建 ItemSegment
    /// </summary>
    public static ItemSegment? FindItemSegment(Node? node)
    {
        while (node != null)
        {
            switch (node)
            {
                case NPower nPower when nPower.Model is { } pm:
                    return ItemLink.EncodePower(pm, pm.Owner);

                case NCardHolder holder when holder.CardModel is { } card:
                    return ItemLink.EncodeCard(card);

                case NPotionHolder potionHolder when potionHolder.Potion is { } potion:
                    return ItemLink.EncodePotion(potion.Model);

                case NRelicInventoryHolder relicHolder when relicHolder.Relic?.Model is { } relic:
                    return ItemLink.EncodeRelic(relic);

                case NCreature nCreature when nCreature.Entity is { } entity:
                    return ItemLink.EncodeTarget(entity);
            }

            node = node.GetParent();
        }

        return null;
    }
}
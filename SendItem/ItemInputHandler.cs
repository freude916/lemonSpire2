using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
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
    ///     从节点树中查找物品并创建 TooltipSegment
    /// </summary>
    public static TooltipSegment? FindItemSegment(Node? node)
    {
        while (node != null)
        {
            switch (node)
            {
                case NPower { Model: { } pm }:
                    return CreatePowerSegment(pm);

                case NCard { Model: { } card }:
                    return CreateCardSegment(card);

                case NCardHolder { CardModel: { } card }:
                    return CreateCardSegment(card);

                case NPotionHolder { Potion: { } potion }:
                    return CreatePotionSegment(potion.Model);

                case NRelicInventoryHolder { Relic.Model: { } relic }:
                    return CreateRelicSegment(relic);

                case NCreature { Entity: { } entity }:
                    return CreateTargetSegment(entity);
            }

            node = node.GetParent();
        }

        return null;
    }

    private static TooltipSegment CreateCardSegment(CardModel card)
    {
        return new TooltipSegment
        {
            Tooltip = CardTooltip.FromModel(card),
            DisplayName = card.Title
        };
    }

    private static TooltipSegment CreatePowerSegment(PowerModel pm)
    {
        return new TooltipSegment
        {
            Tooltip = PowerTooltip.FromModel(pm),
            DisplayName = pm.Title.GetFormattedText()
        };
    }

    private static TooltipSegment CreatePotionSegment(PotionModel potion)
    {
        return new TooltipSegment
        {
            Tooltip = PotionTooltip.FromModel(potion),
            DisplayName = potion.HoverTip.Title ?? potion.Id.Entry
        };
    }

    private static TooltipSegment CreateRelicSegment(RelicModel relic)
    {
        return new TooltipSegment
        {
            Tooltip = RelicTooltip.FromModel(relic),
            DisplayName = relic.HoverTip.Title ?? relic.Id.Entry
        };
    }

    private static TooltipSegment CreateTargetSegment(Creature entity)
    {
        // Use a power tooltip as placeholder for creature display
        var tooltip = new PowerTooltip
        {
            PowerIdStr = "creature",
            Amount = 0
        };
        return new TooltipSegment
        {
            Tooltip = tooltip,
            DisplayName = entity.Name
        };
    }
}
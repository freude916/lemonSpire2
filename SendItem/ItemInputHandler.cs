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
                case NPower nPower when nPower.Model is { } pm:
                    return CreatePowerSegment(pm);

                case NCard nCard when nCard.Model is { } card:
                    return CreateCardSegment(card);

                case NCardHolder holder when holder.CardModel is { } card:
                    return CreateCardSegment(card);

                case NPotionHolder potionHolder when potionHolder.Potion is { } potion:
                    return CreatePotionSegment(potion.Model);

                case NRelicInventoryHolder relicHolder when relicHolder.Relic?.Model is { } relic:
                    return CreateRelicSegment(relic);

                case NCreature nCreature when nCreature.Entity is { } entity:
                    return CreateTargetSegment(entity);
            }

            node = node.GetParent();
        }

        return null;
    }

    private static TooltipSegment CreateCardSegment(CardModel card)
    {
        var tooltip = new CardTooltip
        {
            ModelIdStr = card.Id.Entry,
            UpgradeLevel = card.CurrentUpgradeLevel
        };
        return new TooltipSegment
        {
            Tooltip = tooltip,
            DisplayName = card.Title
        };
    }

    private static TooltipSegment CreatePowerSegment(PowerModel pm)
    {
        var tooltip = new PowerTooltip
        {
            PowerIdStr = pm.Id.Entry,
            Amount = pm.Amount
        };
        return new TooltipSegment
        {
            Tooltip = tooltip,
            DisplayName = pm.Title.GetFormattedText()
        };
    }

    private static TooltipSegment CreatePotionSegment(PotionModel potion)
    {
        var tooltip = new PotionTooltip
        {
            ModelIdStr = potion.Id.Entry
        };
        return new TooltipSegment
        {
            Tooltip = tooltip,
            DisplayName = potion.HoverTip.Title ?? potion.Id.Entry
        };
    }

    private static TooltipSegment CreateRelicSegment(RelicModel relic)
    {
        var tooltip = new RelicTooltip
        {
            ModelIdStr = relic.Id.Entry
        };
        return new TooltipSegment
        {
            Tooltip = tooltip,
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
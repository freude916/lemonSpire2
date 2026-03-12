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
    public static TooltipSegment? FindItemToTooltipSegment(Node? node)
    {
        // 先检查是否点击了附魔标签
        var enchantmentSegment = TryGetEnchantmentFromTab(node);
        if (enchantmentSegment != null)
            return enchantmentSegment;

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

    /// <summary>
    ///     尝试从附魔标签获取附魔信息
    /// </summary>
    private static TooltipSegment? TryGetEnchantmentFromTab(Node? node)
    {
        if (node == null) return null;

        // 检查当前节点或其父节点是否是附魔标签
        var current = node;
        while (current != null)
        {
            // 检查节点名称是否包含 "Enchantment" 或是附魔标签的子节点
            if (current.Name.ToString().Contains("Enchantment", StringComparison.OrdinalIgnoreCase))
            {
                // 向上查找 NCard
                var parent = current.GetParent();
                while (parent != null)
                {
                    if (parent is NCard { Model: { Enchantment: { } enchantment } })
                    {
                        return CreateEnchantmentSegment(enchantment);
                    }

                    parent = parent.GetParent();
                }
            }

            current = current.GetParent();
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

    private static TooltipSegment CreateEnchantmentSegment(EnchantmentModel enchantment)
    {
        return new TooltipSegment
        {
            Tooltip = EnchantmentTooltip.FromModel(enchantment),
            DisplayName = enchantment.Title.GetFormattedText()
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
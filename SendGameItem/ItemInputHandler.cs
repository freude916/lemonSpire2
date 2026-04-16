using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace lemonSpire2.SendGameItem;

/// <summary>
///     物品输入处理器 — 检测 Alt+点击物品并发送消息
/// </summary>
public static class ItemInputHandler
{
    /// <summary>
    ///     从节点树中查找物品并创建 TooltipSegment
    /// </summary>
    public static IEnumerable<TooltipSegment> FindItemToTooltipSegments(Node? node)
    {
        // 先检查是否点击了附魔标签
        var enchantmentSegments = TryGetEnchantmentFromTab(node);
        if (enchantmentSegments.Count > 0)
            return enchantmentSegments;

        // 检查是否是商店槽位
        var merchantSegments = TryGetMerchantItem(node);
        if (merchantSegments.Count > 0)
            return merchantSegments;

        while (node != null)
        {
            switch (node)
            {
                case NDeckHistoryEntry { Card: { } card }:
                    return CardTooltip.FromModel(card).ToTooltipSegments();

                case NEventOptionButton { Option: { } option }:
                    return CreateEventOptionSegments(option);

                case NOrb { Model: { } orb }:
                    return OrbTooltip.FromModel(orb).ToTooltipSegments();

                case NPower { Model: { } pm }:
                    return PowerTooltip.FromModel(pm).ToTooltipSegments();

                case NCardHolder { CardModel: { } card }:
                    return CardTooltip.FromModel(card).ToTooltipSegments();

                case NCard { Model: { } card }:
                    return CardTooltip.FromModel(card).ToTooltipSegments();

                case NPotionHolder { Potion: { } potion }:
                    return PotionTooltip.FromModel(potion.Model).ToTooltipSegments();

                case NPotion { Model: { } potion }:
                    return PotionTooltip.FromModel(potion).ToTooltipSegments();

                case NRelicInventoryHolder { Relic.Model: { } relic }:
                    return RelicTooltip.FromModel(relic).ToTooltipSegments();

                case NRelicBasicHolder { Relic.Model: { } relic }:
                    // 用于 NMultiplayerPlayerExpandedState 中的遗物
                    return RelicTooltip.FromModel(relic).ToTooltipSegments();

                case NRelic { Model: { } relicModel }:
                    // 直接匹配 NRelic 节点
                    return RelicTooltip.FromModel(relicModel).ToTooltipSegments();

                case NCreature { Entity: { } entity }:
                    return CreateTargetSegments(entity);
            }

            node = node.GetParent();
        }

        return [];
    }


    /// <summary>
    ///     尝试从商店槽位获取物品
    /// </summary>
    private static List<TooltipSegment> TryGetMerchantItem(Node? node)
    {
        if (node == null) return [];

        // 向上查找 NMerchantSlot
        var current = node;
        while (current != null)
        {
            if (current is NMerchantSlot { Entry: { } entry })
                return entry switch
                {
                    MerchantCardEntry { CreationResult.Card: { } card } => CardTooltip.FromModel(card)
                        .ToTooltipSegments(),
                    MerchantPotionEntry { Model: { } potion } => PotionTooltip.FromModel(potion).ToTooltipSegments(),
                    MerchantRelicEntry { Model: { } relic } => RelicTooltip.FromModel(relic).ToTooltipSegments(),
                    _ => []
                };

            current = current.GetParent();
        }

        return [];
    }

    /// <summary>
    ///     尝试从附魔标签获取附魔信息
    /// </summary>
    private static List<TooltipSegment> TryGetEnchantmentFromTab(Node? node)
    {
        if (node == null) return [];

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
                    if (parent is NCard { Model.Enchantment: { } enchantment })
                        return EnchantmentTooltip.FromModel(enchantment).ToTooltipSegments();

                    parent = parent.GetParent();
                }
            }

            current = current.GetParent();
        }

        return [];
    }

    private static List<TooltipSegment> CreateEventOptionSegments(EventOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        var segments = new LocTooltip
        {
            Title = option.Title,
            Description = option.Description,
            IsDebuff = false
        }.ToTooltipSegments();

        if (option.Relic != null && option.Relic.Description.LocEntryKey != option.Description.LocEntryKey)
            segments.AddRange(RelicTooltip.FromModel(option.Relic).ToTooltipSegment());

        foreach (var hoverTip in IHoverTip.RemoveDupes(option.HoverTips))
            segments.AddRange(CreateSegmentsFromHoverTip(hoverTip));

        return segments;
    }

    private static List<TooltipSegment> CreateTargetSegments(Creature entity)
    {
        var description = $"{entity.CurrentHp}/{entity.MaxHp}";
        return new RichTextTooltip
        {
            Title = entity.Name,
            Description = description,
            IsDebuff = false,
            IconPath = null
        }.ToTooltipSegments();
    }

    private static List<TooltipSegment> CreateSegmentsFromHoverTip(IHoverTip hoverTip)
    {
        ArgumentNullException.ThrowIfNull(hoverTip);

        if (hoverTip is CardHoverTip cardHoverTip)
            return CardTooltip.FromModel(cardHoverTip.Card).ToTooltipSegments();

        if (hoverTip.CanonicalModel != null)
            return CreateSegmentsFromModel(hoverTip.CanonicalModel);

        if (hoverTip is HoverTip simpleHoverTip)
            return new RichTextTooltip
            {
                Title = simpleHoverTip.Title,
                Description = simpleHoverTip.Description,
                IsDebuff = simpleHoverTip.IsDebuff,
                IconPath = simpleHoverTip.Icon?.ResourcePath
            }.ToTooltipSegments();

        return [];
    }

    private static List<TooltipSegment> CreateSegmentsFromModel(AbstractModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model switch
        {
            CardModel card => CardTooltip.FromModel(card).ToTooltipSegments(),
            PowerModel power => PowerTooltip.FromModel(power).ToTooltipSegments(),
            OrbModel orb => OrbTooltip.FromModel(orb).ToTooltipSegments(),
            PotionModel potion => PotionTooltip.FromModel(potion).ToTooltipSegments(),
            RelicModel relic => RelicTooltip.FromModel(relic).ToTooltipSegments(),
            EnchantmentModel enchantment => EnchantmentTooltip.FromModel(enchantment).ToTooltipSegments(),
            _ => []
        };
    }
}

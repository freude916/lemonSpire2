using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
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
    ///     从节点树中查找主物品并创建单个 TooltipSegment
    /// </summary>
    public static IMsgSegment? FindItemToTooltipSegment(Node? node)
    {
        var enchantmentSegment = TryGetEnchantmentFromTab(node);
        if (enchantmentSegment != null)
            return enchantmentSegment;

        var merchantSegment = TryGetMerchantItem(node);
        if (merchantSegment != null)
            return merchantSegment;

        while (node != null)
        {
            switch (node)
            {
                case NEventOptionButton { Option: { } option }:
                    return CreateEventOptionPrimarySegment(option);
                case NDeckHistoryEntry { Card: { } card }:
                    return CreateSegmentFromModel(card);
                case NOrb { Model: { } orb }:
                    return CreateSegmentFromModel(orb);
                case NCardHolder { CardModel: { } card }:
                    return CreateSegmentFromModel(card);
                case NCard { Model: { } card }:
                    return CreateSegmentFromModel(card);
                case NPower { Model: { } pm }:
                    return CreatePowerShareSegment(pm); // Power 必须小于 Orb
                case NPotionHolder { Potion.Model: { } potion }:
                    return CreateSegmentFromModel(potion);
                case NPotion { Model: { } potion }:
                    return CreateSegmentFromModel(potion);
                case NRelicInventoryHolder { Relic.Model: { } relic }:
                    return CreateSegmentFromModel(relic);
                case NRelicBasicHolder { Relic.Model: { } relic }:
                    return CreateSegmentFromModel(relic);
                case NRelic { Model: { } relicModel }:
                    return CreateSegmentFromModel(relicModel);
                case NCreature { Entity: { } entity }:
                    return CreateTargetSegment(entity);
            }

            node = node.GetParent();
        }

        return null;
    }

    /// <summary>
    ///     从节点树中查找物品并创建主 tooltip + hovertips
    /// </summary>
    public static IReadOnlyList<IMsgSegment> FindItemAndHoverTipSegments(Node? node)
    {
        var enchantmentSegments = TryGetEnchantmentSegmentsFromTab(node);
        if (enchantmentSegments.Count > 0)
            return enchantmentSegments;

        var merchantSegments = TryGetMerchantItemAndHoverTipSegments(node);
        if (merchantSegments.Count > 0)
            return merchantSegments;

        while (node != null)
        {
            switch (node)
            {
                case NEventOptionButton { Option: { } option }:
                    return CreateEventOptionSegments(option);

                case NDeckHistoryEntry { Card: { } card }:
                    return CreateItemAndHoverTipSegments(card);

                case NOrb { Model: { } orb }:
                    return CreateItemAndHoverTipSegments(orb);

                case NPower { Model: { } pm }:
                    return CreateItemAndHoverTipSegments(pm);

                case NCardHolder { CardModel: { } card }:
                    return CreateItemAndHoverTipSegments(card);

                case NCard { Model: { } card }:
                    return CreateItemAndHoverTipSegments(card);

                case NPotionHolder { Potion: { } potion }:
                    return CreateItemAndHoverTipSegments(potion.Model);

                case NPotion { Model: { } potion }:
                    return CreateItemAndHoverTipSegments(potion);

                case NRelicInventoryHolder { Relic.Model: { } relic }:
                    return CreateItemAndHoverTipSegments(relic);

                case NRelicBasicHolder { Relic.Model: { } relic }:
                    return CreateItemAndHoverTipSegments(relic);

                case NRelic { Model: { } relicModel }:
                    return CreateItemAndHoverTipSegments(relicModel);

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
    private static TooltipSegment? TryGetMerchantItem(Node? node)
    {
        if (node == null) return null;

        var current = node;
        while (current != null)
        {
            if (current is NMerchantSlot { Entry: { } entry })
                return entry switch
                {
                    MerchantCardEntry { CreationResult.Card: { } card } => CreateSegmentFromModel(card),
                    MerchantPotionEntry { Model: { } potion } => CreateSegmentFromModel(potion),
                    MerchantRelicEntry { Model: { } relic } => CreateSegmentFromModel(relic),
                    _ => null
                };

            current = current.GetParent();
        }

        return null;
    }

    private static List<TooltipSegment> TryGetMerchantItemAndHoverTipSegments(Node? node)
    {
        if (node == null) return [];

        var current = node;
        while (current != null)
        {
            if (current is NMerchantSlot { Entry: { } entry })
                return entry switch
                {
                    MerchantCardEntry { CreationResult.Card: { } card } => CreateItemAndHoverTipSegments(card),
                    MerchantPotionEntry { Model: { } potion } => CreateItemAndHoverTipSegments(potion),
                    MerchantRelicEntry { Model: { } relic } => CreateItemAndHoverTipSegments(relic),
                    _ => []
                };

            current = current.GetParent();
        }

        return [];
    }

    /// <summary>
    ///     尝试从附魔标签获取附魔信息
    /// </summary>
    private static TooltipSegment? TryGetEnchantmentFromTab(Node? node)
    {
        if (node == null) return null;

        var current = node;
        while (current != null)
        {
            if (current.Name.ToString().Contains("Enchantment", StringComparison.OrdinalIgnoreCase))
            {
                var parent = current.GetParent();
                while (parent != null)
                {
                    if (parent is NCard { Model.Enchantment: { } enchantment })
                        return CreateSegmentFromModel(enchantment);

                    parent = parent.GetParent();
                }
            }

            current = current.GetParent();
        }

        return null;
    }

    private static List<TooltipSegment> TryGetEnchantmentSegmentsFromTab(Node? node)
    {
        if (node == null) return [];

        var current = node;
        while (current != null)
        {
            if (current.Name.ToString().Contains("Enchantment", StringComparison.OrdinalIgnoreCase))
            {
                var parent = current.GetParent();
                while (parent != null)
                {
                    if (parent is NCard { Model.Enchantment: { } enchantment })
                        return CreateItemAndHoverTipSegments(enchantment);

                    parent = parent.GetParent();
                }
            }

            current = current.GetParent();
        }

        return [];
    }

    private static List<IMsgSegment> CreateEventOptionSegments(EventOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        var segments = new List<IMsgSegment> { CreateEventOptionPrimarySegment(option) };

        if (option.Relic != null && option.Relic.Description.LocEntryKey != option.Description.LocEntryKey)
            segments.Add(CreateSegmentFromModel(option.Relic));

        foreach (var hoverTip in IHoverTip.RemoveDupes(option.HoverTips))
            segments.AddRange(CreateSegmentsFromHoverTip(hoverTip));

        return segments;
    }

    private static TooltipSegment CreateEventOptionPrimarySegment(EventOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        return new LocTooltip
        {
            Title = option.Title,
            Description = option.Description,
            IsDebuff = false
        }.ToTooltipSegment();
    }

    private static IReadOnlyList<IMsgSegment> CreateTargetSegments(Creature entity)
    {
        return [CreateTargetSegment(entity), .. CreateSegmentsFromHoverTips(entity.HoverTips)];
    }

    private static EntitySegment CreateTargetSegment(Creature entity)
    {
        return EntitySegment.FromCreature(entity);
    }

    private static TemplateSegment CreatePowerShareSegment(PowerModel power)
    {
        ArgumentNullException.ThrowIfNull(power);

        var ownerSegment = CreateTargetSegment(power.Owner);

        return new TemplateSegment
        {
            Template = new LocString("gameplay_ui", "LEMONSPIRE.chat.ownedItem"),
            Slots =
            [
                ownerSegment.ToNamedSegment("Owner"),
                CreateSegmentFromModel(power).ToNamedSegment("Item")
            ]
        };
    }

    private static List<TooltipSegment> CreateItemAndHoverTipSegments(AbstractModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var segments = new List<TooltipSegment> { CreateSegmentFromModel(model) };
        segments.AddRange(model switch
        {
            CardModel card => CreateSegmentsFromHoverTips(card.HoverTips),
            PowerModel power => CreateSegmentsFromHoverTips(power.HoverTips),
            OrbModel orb => CreateSegmentsFromHoverTips(orb.HoverTips),
            PotionModel potion => CreateSegmentsFromHoverTips(potion.HoverTips),
            RelicModel relic => CreateSegmentsFromHoverTips(relic.HoverTipsExcludingRelic),
            EnchantmentModel enchantment => CreateSegmentsFromHoverTips(enchantment.HoverTips),
            _ => []
        });
        return segments;
    }

    private static List<TooltipSegment> CreateSegmentsFromHoverTip(IHoverTip hoverTip, bool allowNoLoc = false)
    {
        ArgumentNullException.ThrowIfNull(hoverTip);

        if (hoverTip is CardHoverTip cardHoverTip)
            return [CardTooltip.FromModel(cardHoverTip.Card).ToTooltipSegment()];

        if (hoverTip.CanonicalModel != null)
            return [CreateSegmentFromModel(hoverTip.CanonicalModel)];

        if (hoverTip is not HoverTip simpleHoverTip) return [];
        if (allowNoLoc)
            return
            [
                new RichTextTooltip
                {
                    Title = simpleHoverTip.Title,
                    Description = simpleHoverTip.Description,
                    IsDebuff = simpleHoverTip.IsDebuff,
                    IconPath = simpleHoverTip.Icon?.ResourcePath
                }.ToTooltipSegment()
            ];
        return [];
    }

    private static List<TooltipSegment> CreateSegmentsFromHoverTips(IEnumerable<IHoverTip> hoverTips)
    {
        var segments = new List<TooltipSegment>();
        foreach (var hoverTip in IHoverTip.RemoveDupes(hoverTips))
            segments.AddRange(CreateSegmentsFromHoverTip(hoverTip, true));
        return segments;
    }

    private static TooltipSegment CreateSegmentFromModel(AbstractModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model switch
        {
            CardModel card => CardTooltip.FromModel(card).ToTooltipSegment(),
            PowerModel power => PowerTooltip.FromModel(power).ToTooltipSegment(),
            OrbModel orb => OrbTooltip.FromModel(orb).ToTooltipSegment(),
            PotionModel potion => PotionTooltip.FromModel(potion).ToTooltipSegment(),
            RelicModel relic => RelicTooltip.FromModel(relic).ToTooltipSegment(),
            EnchantmentModel enchantment => EnchantmentTooltip.FromModel(enchantment).ToTooltipSegment(),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model.GetType().Name, "Unsupported model type")
        };
    }
}

using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace lemonSpire2.Tooltips;

public sealed class CardTooltip : Tooltip
{
    protected override string TypeTag => "card";

    public required string ModelIdStr { get; set; }
    public int UpgradeLevel { get; set; }

    public static CardTooltip FromModel(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return new CardTooltip
        {
            ModelIdStr = card.Id.Entry,
            UpgradeLevel = card.CurrentUpgradeLevel
        };
    }

    public static Color GetCardRarityColor(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => StsColors.cardTitleOutlineCommon,
            CardRarity.Common => StsColors.cardTitleOutlineCommon,
            CardRarity.Uncommon => StsColors.cardTitleOutlineUncommon,
            CardRarity.Rare => StsColors.cardTitleOutlineRare,
            CardRarity.Curse => StsColors.cardTitleOutlineCurse,
            CardRarity.Quest => StsColors.cardTitleOutlineQuest,
            CardRarity.Status => StsColors.cardTitleOutlineStatus,
            CardRarity.Ancient => StsColors.cardTitleOutlineSpecial,
            CardRarity.Event => StsColors.cardTitleOutlineSpecial,
            CardRarity.Token => StsColors.cardTitleOutlineSpecial,
            CardRarity.None => StsColors.cream,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null)
        };
    }

    public static Color GetCardPoolColor(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.VisualCardPool.DeckEntryCardColor;
    }

    public override string Render()
    {
        var card = ResolveModel();
        if (card is null) return "Broken Card";

        var rarityColor = GetCardRarityColor(card.Rarity);
        var poolColor = GetCardPoolColor(card);

        return $"[color={poolColor.ToHtml()}]■[/color] [color={rarityColor.ToHtml()}]{card.Title}[/color]";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(ModelIdStr);
        writer.WriteInt(UpgradeLevel);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ModelIdStr = reader.ReadString();
        UpgradeLevel = reader.ReadInt();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        if (model is null) return null;


        var container = PreloadManager.Cache
            .GetScene("res://scenes/ui/card_hover_tip.tscn")
            .Instantiate<Control>();

        var nCard = container.GetNode<NCard>("%Card");

        // Must AddChild before UpdateVisuals, use CallDeferred
        container.TreeEntered += () =>
        {
            Callable.From(() =>
            {
                nCard.Model = model;
                nCard.UpdateVisuals(PileType.Deck, CardPreviewMode.Normal);
            }).CallDeferred();
        };

        SetSubtreeMouseIgnore(container);
        return container;
    }

    public override IHoverTip ToHoverTip()
    {
        var model = ResolveModel();
        if (model is null)
            throw new InvalidOperationException($"Cannot resolve card model: {ModelIdStr}");

        var mutableCard = model.IsMutable ? model : model.ToMutable();
        return new CardHoverTip(mutableCard);
    }

    private CardModel? ResolveModel()
    {
        return StsUtil.ResolveModel<CardModel>(ModelIdStr);
    }
}

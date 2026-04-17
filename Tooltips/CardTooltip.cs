using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace lemonSpire2.Tooltips;

public sealed class CardTooltip : Tooltip
{
    protected override string TypeTag => "card";

    public required SerializableCard Snapshot { get; set; }
    public ulong OwnerNetId { get; set; }
    public PileType DisplayPile { get; set; } = PileType.Deck;
    public bool UseCombatPreview { get; set; }

    public static CardTooltip FromModel(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var snapshotSource = (CardModel)card.MutableClone();
        var ownerNetId = snapshotSource.Owner.NetId;
        var pileType = snapshotSource.Pile?.Type ?? PileType.Deck;
        var useCombatPreview = snapshotSource.IsInCombat;

        return new CardTooltip
        {
            Snapshot = snapshotSource.ToSerializable(),
            OwnerNetId = ownerNetId,
            DisplayPile = pileType,
            UseCombatPreview = useCombatPreview
        };
    }

    public static CardTooltip FromChatReference(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var snapshotSource = (CardModel)card.MutableClone();

        return new CardTooltip
        {
            Snapshot = snapshotSource.ToSerializable(),
            UseCombatPreview = false
        };
    }

    public static Color GetCardPoolColor(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return card.VisualCardPool.DeckEntryCardColor;
    }

    public override string Render()
    {
        var card = ResolveCardForChat();
        if (card is null) return "Broken Card";

        var rarityColor = StsUtil.GetRarityColor(card.Rarity);
        var poolColor = GetCardPoolColor(card);

        return $"[color={poolColor.ToHtml()}]■[/color] [color={rarityColor.ToHtml()}]{card.Title}[/color]";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(Snapshot);
        writer.WriteULong(OwnerNetId);
        writer.WriteInt((int)DisplayPile);
        writer.WriteBool(UseCombatPreview);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        Snapshot = reader.Read<SerializableCard>();
        OwnerNetId = reader.ReadULong();
        var pile = reader.ReadInt();
        DisplayPile = Enum.IsDefined(typeof(PileType), pile) ? (PileType)pile : PileType.Deck;
        UseCombatPreview = reader.ReadBool();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveCardForChat();
        if (model is null) return null;
        var previewPile = ResolvePreviewPile(model);
        var root = new HBoxContainer
        {
            Name = "CardTooltipPreview"
        };
        root.AddThemeConstantOverride("separation", 10);

        var container = PreloadManager.Cache
            .GetScene("res://scenes/ui/card_hover_tip.tscn")
            .Instantiate<Control>();

        var nCard = container.GetNode<NCard>("%Card");
        var tips = model.HoverTips;

        // Must AddChild before UpdateVisuals, use CallDeferred.
        container.TreeEntered += () =>
        {
            Callable.From(() =>
            {
                nCard.Model = model;
                nCard.UpdateVisuals(previewPile, CardPreviewMode.Normal);
                NHoverTipSet.CreateAndShow(container, tips, HoverTipAlignment.Right);
            }).CallDeferred();
        };
        root.AddChild(container);

        SetSubtreeMouseIgnore(root);
        return root;
    }

    private CardModel? ResolveCardForChat()
    {
        if (Snapshot.Id == null) return null;

        var model = CardModel.FromSerializable(Snapshot);
        var owner = RunManager.Instance.State?.GetPlayer(OwnerNetId);
        if (owner == null) return model;
        model.Owner = owner;
        if (UseCombatPreview) model.UpgradePreviewType = CardUpgradePreviewType.Combat;

        return model;
    }

    private PileType ResolvePreviewPile(CardModel model)
    {
        if (DisplayPile is not (PileType.Hand or PileType.Play)) return DisplayPile;
        return model.RunState == null ? PileType.Deck : DisplayPile;
    }
}

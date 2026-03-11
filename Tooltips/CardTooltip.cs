using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        return new CardTooltip
        {
            ModelIdStr = card.Id.Entry,
            UpgradeLevel = card.CurrentUpgradeLevel
        };
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

        try
        {
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
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to create card preview: {ex.Message}");
            throw;
        }
    }

    private CardModel? ResolveModel()
    {
        foreach (var card in ModelDb.AllCards)
            if (card.Id.Entry == ModelIdStr)
                return card;
        return null;
    }

    private static void SetSubtreeMouseIgnore(Node node)
    {
        if (node is Control c)
            c.MouseFilter = Control.MouseFilterEnum.Ignore;

        foreach (var child in node.GetChildren())
            SetSubtreeMouseIgnore(child);
    }
}
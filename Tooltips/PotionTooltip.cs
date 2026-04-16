using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class PotionTooltip : Tooltip
{
    protected override string TypeTag => "potion";

    public required string ModelIdStr { get; set; }

    public static PotionTooltip FromModel(PotionModel potion)
    {
        ArgumentNullException.ThrowIfNull(potion);
        return new PotionTooltip
        {
            ModelIdStr = potion.Id.Entry
        };
    }

    public static Color GetPotionRarityColor(PotionRarity rarity)
    {
        return rarity switch
        {
            // potion don't have rarity color definations in the game, so we will just use the same colors as cards
            PotionRarity.Common => StsColors.cardTitleOutlineCommon,
            PotionRarity.Uncommon => StsColors.cardTitleOutlineUncommon,
            PotionRarity.Rare => StsColors.cardTitleOutlineRare,
            PotionRarity.Event => StsColors.cardTitleOutlineSpecial,
            PotionRarity.Token => StsColors.cardTitleOutlineSpecial,
            PotionRarity.None => StsColors.cream,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null)
        };
    }

    public override string Render()
    {
        var model = ResolveModel();
        if (model is null) return "Broken Potion";

        var color = GetPotionRarityColor(model.Rarity);
        var iconPath = model.ImagePath;

        return $"[img={16}x{16}]{iconPath}[/img] [color={color.ToHtml()}]{model.Title.GetFormattedText()}[/color]";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(ModelIdStr);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ModelIdStr = reader.ReadString();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        return model is null ? null : BuildHoverTipControl(model.HoverTip, model.Image);
        // use a more stable image object
    }

    private PotionModel? ResolveModel()
    {
        return StsUtil.ResolveModel<PotionModel>(ModelIdStr);
    }
}

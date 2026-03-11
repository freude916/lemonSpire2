using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class RelicTooltip : Tooltip
{
    protected override string TypeTag => "relic";

    public required string ModelIdStr { get; set; }

    public static RelicTooltip FromModel(RelicModel relic)
    {
        ArgumentNullException.ThrowIfNull(relic);
        return new RelicTooltip
        {
            ModelIdStr = relic.Id.Entry
        };
    }

    public static Color GetRelicRarityColor(RelicRarity rarity)
    {
        return rarity switch
        {
            RelicRarity.Starter => StsColors.cardTitleOutlineCommon,
            RelicRarity.Common => StsColors.cardTitleOutlineCommon,
            RelicRarity.Uncommon => StsColors.cardTitleOutlineUncommon,
            RelicRarity.Rare => StsColors.cardTitleOutlineRare,
            RelicRarity.Shop => StsColors.cardTitleOutlineSpecial,
            RelicRarity.Event => StsColors.cardTitleOutlineSpecial,
            RelicRarity.Ancient => StsColors.cardTitleOutlineSpecial,
            RelicRarity.None => StsColors.cream,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null)
        };
    }

    public override string Render()
    {
        var model = ResolveModel();
        if (model is null) return "Broken Relic";

        var color = GetRelicRarityColor(model.Rarity);
        var iconPath = model.IconPath;

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
        if (model is null) return null;

        return BuildHoverTipControl(model.HoverTip, model.Icon);
    }

    public override IHoverTip ToHoverTip()
    {
        var model = ResolveModel();
        if (model is null)
            throw new InvalidOperationException($"Cannot resolve relic model: {ModelIdStr}");

        return model.HoverTip;
    }

    private RelicModel? ResolveModel()
    {
        return StsUtil.ResolveModel<RelicModel>(ModelIdStr);
    }
}

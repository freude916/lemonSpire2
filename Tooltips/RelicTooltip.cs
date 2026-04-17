using Godot;
using lemonSpire2.util;
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

    public override string Render()
    {
        var model = ResolveModel();
        if (model is null) return "Broken Relic";

        var color = StsUtil.GetRarityColor(model.Rarity);
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
        return model is null ? null : BuildHoverTipControl(model.HoverTip, model.Icon);
    }

    private RelicModel? ResolveModel()
    {
        return StsUtil.ResolveModel<RelicModel>(ModelIdStr);
    }
}

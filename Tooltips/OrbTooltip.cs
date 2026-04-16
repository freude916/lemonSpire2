using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class OrbTooltip : Tooltip
{
    protected override string TypeTag => "orb";

    public required string OrbIdStr { get; set; }
    public double PassiveVal { get; set; }
    public double EvokeVal { get; set; }

    public static OrbTooltip FromModel(OrbModel orb)
    {
        ArgumentNullException.ThrowIfNull(orb);
        return new OrbTooltip
        {
            OrbIdStr = orb.Id.Entry,
            PassiveVal = (double)orb.PassiveVal,
            EvokeVal = (double)orb.EvokeVal
        };
    }

    public override string Render()
    {
        var orb = ResolveModel();
        if (orb is null) return "Broken Orb";

        var title = orb.Title.GetFormattedText();
        var iconPath = orb.IconPath;

        return $"[img={16}x{16}]{iconPath}[/img] [color=#88ccff]{title}[/color]";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(OrbIdStr);
        writer.WriteDouble(PassiveVal);
        writer.WriteDouble(EvokeVal);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        OrbIdStr = reader.ReadString();
        PassiveVal = reader.ReadDouble();
        EvokeVal = reader.ReadDouble();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        return model is null ? null : BuildHoverTipControl(model.DumbHoverTip, model.Icon);
    }

    private OrbModel? ResolveModel()
    {
        return StsUtil.ResolveModel<OrbModel>(OrbIdStr);
    }
}

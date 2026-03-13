using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class PowerTooltip : Tooltip
{
    protected override string TypeTag => "power";

    public required string PowerIdStr { get; set; }
    public int Amount { get; set; }
    public bool IsPlayer { get; set; }
    public string ApplierName { get; set; } = "";

    public static PowerTooltip FromModel(PowerModel power)
    {
        ArgumentNullException.ThrowIfNull(power);
        return new PowerTooltip
        {
            PowerIdStr = power.Id.Entry,
            Amount = power.Amount
        };
    }

    public static Color GetPowerColor(PowerModel power)
    {
        ArgumentNullException.ThrowIfNull(power);
        return power.AmountLabelColor;
    }

    public override string Render()
    {
        var power = ResolveModel();
        if (power is null) return "Broken Power";

        var color = GetPowerColor(power);
        var title = power.Title.GetFormattedText();
        var iconPath = power.IconPath;

        return $"[img={16}x{16}]{iconPath}[/img] [color={color.ToHtml()}]{title}[/color]";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(PowerIdStr);
        writer.WriteInt(Amount);
        writer.WriteBool(IsPlayer);
        writer.WriteString(ApplierName);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        PowerIdStr = reader.ReadString();
        Amount = reader.ReadInt();
        IsPlayer = reader.ReadBool();
        ApplierName = reader.ReadString();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        if (model is null) return null;

        return BuildHoverTipControl(model.DumbHoverTip, model.Icon);
    }

    public override IHoverTip ToHoverTip()
    {
        var model = ResolveModel();
        if (model is null)
            throw new InvalidOperationException($"Cannot resolve power model: {PowerIdStr}");

        return model.DumbHoverTip;
    }

    private PowerModel? ResolveModel()
    {
        return StsUtil.ResolveModel<PowerModel>(PowerIdStr);
    }
}

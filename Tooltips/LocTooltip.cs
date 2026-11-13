using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

/// <summary>
///     Tooltip extracted from LocString
/// </summary>
public sealed class LocTooltip : Tooltip
{
    protected override string TypeTag => "lt";

    public required LocString Title { get; set; }
    public required LocString Description { get; set; }
    public bool IsDebuff { get; set; }

    public override string Render()
    {
        return $"{Title.GetFormattedText()}";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(Title.LocTable);
        writer.WriteString(Title.LocEntryKey);
        writer.WriteString(Description.LocTable);
        writer.WriteString(Description.LocEntryKey);
        writer.WriteBool(IsDebuff);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var titleTable = reader.ReadString();
        var titleEntryKey = reader.ReadString();
        var descriptionTable = reader.ReadString();
        var descriptionEntryKey = reader.ReadString();
        Title = new LocString(titleTable, titleEntryKey);
        Description = new LocString(descriptionTable, descriptionEntryKey);
        IsDebuff = reader.ReadBool();
    }

    public override Control? CreatePreview()
    {
        return BuildHoverTipControl(Title.GetFormattedText(), Description.GetFormattedText(), IsDebuff);
    }
}

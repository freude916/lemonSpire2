using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public record LocSegment : IMsgSegment
{
    // Required by Activator.CreateInstance during network deserialization.
    public LocSegment()
    {
        LocTable = string.Empty;
        LocEntryKey = string.Empty;
    }

    public LocSegment(LocString locString)
    {
        ArgumentNullException.ThrowIfNull(locString);
        LocTable = locString.LocTable;
        LocEntryKey = locString.LocEntryKey;
    }

    public LocSegment(string locTable, string locEntryKey)
    {
        ArgumentNullException.ThrowIfNull(locTable);
        ArgumentNullException.ThrowIfNull(locEntryKey);
        LocTable = locTable;
        LocEntryKey = locEntryKey;
    }

    public string LocTable { get; set; }
    public string LocEntryKey { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(LocTable);
        writer.WriteString(LocEntryKey);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        LocTable = reader.ReadString();
        LocEntryKey = reader.ReadString();
    }

    public void RenderTo(RichTextLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        label.AppendText(new LocString(LocTable, LocEntryKey).GetFormattedText());
    }

    public string Render()
    {
        return new LocString(LocTable, LocEntryKey).GetFormattedText();
    }
}

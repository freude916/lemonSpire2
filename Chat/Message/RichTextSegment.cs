using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public record RichTextSegment : IMsgSegment
{
    public required string Text { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(Text);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        Text = reader.ReadString();
    }

    public void RenderTo(RichTextLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        label.AppendText(Text);
    }

    public string Render()
    {
        return Text;
    }
}

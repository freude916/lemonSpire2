using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public sealed record TextDisplaySegment : IMsgSegment
{
    public string HeaderText { get; set; } = "System";
    public required string Text { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(HeaderText);
        writer.WriteString(Text);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        HeaderText = reader.ReadString();
        Text = reader.ReadString();
    }

    public string Render()
    {
        return Text;
    }
}

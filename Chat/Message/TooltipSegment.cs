using Godot;
using lemonSpire2.Tooltips;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public record TooltipSegment : IMsgSegment
{
    public required Tooltip Tooltip { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(Tooltip.GetType().AssemblyQualifiedName!);
        Tooltip.Serialize(writer);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var typeName = reader.ReadString();
        var type = Type.GetType(typeName);
        if (type is null || !typeof(Tooltip).IsAssignableFrom(type))
            throw new InvalidOperationException($"Unknown tooltip type: {typeName}");

        Tooltip = (Tooltip)Activator.CreateInstance(type)!;
        Tooltip.Deserialize(reader);
    }

    public void RenderTo(RichTextLabel label)
    {
        ArgumentNullException.ThrowIfNull(label);
        var meta = Tooltip.ToMetaString();
        label.PushMeta(meta);
        label.AppendText("[lb]");
        label.AppendText(Tooltip.Render());
        label.AppendText("[rb]");
        label.Pop();
    }

    public string Render()
    {
        var meta = Tooltip.ToMetaString();
        return $"[meta={meta}]{Tooltip.Render()}[/meta]";
    }
}

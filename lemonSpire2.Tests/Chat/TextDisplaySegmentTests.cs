using lemonSpire2.Chat.Message;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using Xunit;

namespace lemonSpire2.Tests.Chat;

public sealed class TextDisplaySegmentTests
{
    [Fact]
    public void Render_ShouldReturnBodyText()
    {
        var segment = new TextDisplaySegment
        {
            HeaderText = "Help",
            Text = "Line 1\nLine 2"
        };

        Assert.Equal("Line 1\nLine 2", segment.Render());
    }

    [Fact]
    public void SerializeRoundTrip_ShouldPreserveHeaderAndText()
    {
        var original = new TextDisplaySegment
        {
            HeaderText = "System",
            Text = "/help - 显示可用聊天命令"
        };

        var writer = new PacketWriter();
        original.Serialize(writer);

        var reader = new PacketReader();
        reader.Reset(writer.Buffer);

        var copy = new TextDisplaySegment { Text = string.Empty };
        copy.Deserialize(reader);

        Assert.Equal(original.HeaderText, copy.HeaderText);
        Assert.Equal(original.Text, copy.Text);
    }
}

using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public interface IMsgSegment : IPacketSerializable
{
    /// <summary>
    ///     Renders this segment as BBCode text for display in RichTextLabel.
    /// </summary>
    string Render();
}
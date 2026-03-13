using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public interface IMsgSegment : IPacketSerializable
{
    /// <summary>
    ///     Renders this segment directly to a RichTextLabel using PushXxx APIs.
    /// </summary>
    void RenderTo(RichTextLabel label);

    /// <summary>
    ///     Renders this segment as BBCode text (for serialization/debugging).
    /// </summary>
    string Render();
}

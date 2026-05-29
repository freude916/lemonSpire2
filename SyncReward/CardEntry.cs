using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace lemonSpire2.SyncReward;

/// <summary>
///     卡牌条目
/// </summary>
public record CardEntry
{
    public SerializableCard Snapshot { get; set; } = new();

    public static CardEntry FromModel(CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var snapshotSource = card.IsMutable ? card : (CardModel)card.MutableClone();
        return new CardEntry
        {
            Snapshot = snapshotSource.ToSerializable()
        };
    }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(Snapshot);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        Snapshot = reader.Read<SerializableCard>();
    }
}

using lemonSpire2.util.Net;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace lemonSpire2.PlayerStateEx.RemoteFlash;

public sealed record RemoteUiFlashMessage : BasePlayerMessage
{
    public ulong TargetPlayerId { get; set; }

    public RemoteUiFlashKind Kind { get; set; }

    public SerializableCard? Card { get; set; }

    public SerializablePotion? Potion { get; set; }

    public SerializableRelic? Relic { get; set; }

    public uint ShopEntryId { get; set; }

    public RunLocation ShopLocation { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteULong(SenderId);
        writer.WriteULong(TargetPlayerId);
        writer.WriteInt((int)Kind, 8);

        writer.WriteBool(Card != null);
        if (Card != null) writer.Write(Card);

        writer.WriteBool(Potion != null);
        if (Potion != null) writer.Write(Potion);

        writer.WriteBool(Relic != null);
        if (Relic != null) writer.Write(Relic);

        if (Kind == RemoteUiFlashKind.BlitzShopEntry)
        {
            writer.WriteUInt(ShopEntryId);
            writer.Write(ShopLocation);
        }
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        SenderId = reader.ReadULong();
        TargetPlayerId = reader.ReadULong();

        var kindValue = reader.ReadInt(8);
        Kind = Enum.IsDefined(typeof(RemoteUiFlashKind), kindValue)
            ? (RemoteUiFlashKind)kindValue
            : RemoteUiFlashKind.HandCard;

        Card = reader.ReadBool() ? reader.Read<SerializableCard>() : null;
        Potion = reader.ReadBool() ? reader.Read<SerializablePotion>() : null;
        Relic = reader.ReadBool() ? reader.Read<SerializableRelic>() : null;

        if (Kind == RemoteUiFlashKind.BlitzShopEntry)
        {
            ShopEntryId = reader.ReadUInt();
            ShopLocation = reader.Read<RunLocation>();
        }
    }
}

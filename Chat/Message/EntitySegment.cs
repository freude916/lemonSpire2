using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public sealed record EntitySegment : IMsgSegment
{
    public enum EntityKind
    {
        Unknown = 0,
        Player = 1,
        Creature = 2
    }

    public enum NameKind
    {
        Plain = 0,
        Localized = 1
    }

    private const string MetaPrefix = "entity";

    public EntityKind Kind { get; set; }
    public NameKind DisplayNameKind { get; set; }
    public ulong PlayerNetId { get; set; }
    public uint CreatureCombatId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string NameLocTable { get; set; } = string.Empty;
    public string NameLocEntryKey { get; set; } = string.Empty;
    public bool HasHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }

    public void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteInt((int)Kind);
        writer.WriteInt((int)DisplayNameKind);
        writer.WriteULong(PlayerNetId);
        writer.WriteUInt(CreatureCombatId);
        writer.WriteString(DisplayName);
        writer.WriteString(NameLocTable);
        writer.WriteString(NameLocEntryKey);
        writer.WriteBool(HasHp);
        writer.WriteInt(CurrentHp);
        writer.WriteInt(MaxHp);
    }

    public void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var kind = reader.ReadInt();
        Kind = Enum.IsDefined(typeof(EntityKind), kind)
            ? (EntityKind)kind
            : EntityKind.Unknown;

        var nameKind = reader.ReadInt();
        DisplayNameKind = Enum.IsDefined(typeof(NameKind), nameKind)
            ? (NameKind)nameKind
            : NameKind.Plain;

        PlayerNetId = reader.ReadULong();
        CreatureCombatId = reader.ReadUInt();
        DisplayName = reader.ReadString();
        NameLocTable = reader.ReadString();
        NameLocEntryKey = reader.ReadString();
        HasHp = reader.ReadBool();
        CurrentHp = reader.ReadInt();
        MaxHp = reader.ReadInt();
    }

    public string Render()
    {
        var text = RenderText();
        return $"[url={ToMetaString()}]{text}[/url]";
    }

    public string RenderText()
    {
        var name = RenderName();
        return HasHp ? $"{name} {CurrentHp}/{MaxHp}" : name;
    }

    public string RenderName()
    {
        return DisplayNameKind == NameKind.Localized
            ? new LocString(NameLocTable, NameLocEntryKey).GetFormattedText()
            : DisplayName;
    }

    public string ToMetaString()
    {
        return Kind switch
        {
            EntityKind.Player => $"{MetaPrefix}:p:{PlayerNetId}",
            EntityKind.Creature => $"{MetaPrefix}:c:{CreatureCombatId}",
            _ => $"{MetaPrefix}:u:0"
        };
    }

    public static bool IsEntityMeta(string meta)
    {
        ArgumentNullException.ThrowIfNull(meta);
        return meta.StartsWith($"{MetaPrefix}:", StringComparison.Ordinal);
    }

    public static bool TryParseMeta(string meta, out EntityKind kind, out ulong playerNetId, out uint creatureCombatId)
    {
        ArgumentNullException.ThrowIfNull(meta);
        kind = EntityKind.Unknown;
        playerNetId = 0;
        creatureCombatId = 0;

        var parts = meta.Split(':', 3);
        if (parts is not [MetaPrefix, _, _])
            return false;

        switch (parts[1])
        {
            case "p" when ulong.TryParse(parts[2], out playerNetId):
                kind = EntityKind.Player;
                return true;
            case "c" when uint.TryParse(parts[2], out creatureCombatId):
                kind = EntityKind.Creature;
                return true;
            default:
                return false;
        }
    }

    public static EntitySegment FromPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return new EntitySegment
        {
            Kind = EntityKind.Player,
            DisplayNameKind = NameKind.Plain,
            PlayerNetId = player.NetId,
            DisplayName = StsUtil.GetPlayerNameFromNetId(player.NetId),
            HasHp = true,
            CurrentHp = player.Creature.CurrentHp,
            MaxHp = player.Creature.MaxHp
        };
    }

    public static EntitySegment FromCreature(Creature creature)
    {
        ArgumentNullException.ThrowIfNull(creature);

        if (!creature.CombatId.HasValue)
            throw new InvalidOperationException("Creature entity segment requires CombatId.");

        if (creature is { IsPlayer: true, Player: not null })
            return FromPlayer(creature.Player);

        if (creature is { Monster: not null })
            return new EntitySegment
            {
                Kind = EntityKind.Creature,
                DisplayNameKind = NameKind.Localized,
                CreatureCombatId = creature.CombatId.Value,
                NameLocTable = creature.Monster.Title.LocTable,
                NameLocEntryKey = creature.Monster.Title.LocEntryKey,
                HasHp = true,
                CurrentHp = creature.CurrentHp,
                MaxHp = creature.MaxHp
            };

        return new EntitySegment
        {
            Kind = EntityKind.Creature,
            DisplayNameKind = NameKind.Plain,
            CreatureCombatId = creature.CombatId.Value,
            DisplayName = creature.Name,
            HasHp = true,
            CurrentHp = creature.CurrentHp,
            MaxHp = creature.MaxHp
        };
    }
}

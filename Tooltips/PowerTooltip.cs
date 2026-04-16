using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.Tooltips;

public sealed class PowerTooltip : Tooltip
{
    public enum TooltipOwnerKind
    {
        None = 0,
        PlayerName = 1,
        MonsterModel = 2
    }

    protected override string TypeTag => "power";

    public required string PowerIdStr { get; set; }
    public int Amount { get; set; }
    public TooltipOwnerKind OwnerKind { get; set; } = TooltipOwnerKind.None;
    public string OwnerPlayerName { get; set; } = "";
    public string OwnerMonsterIdStr { get; set; } = "";

    public static PowerTooltip FromModel(PowerModel power)
    {
        ArgumentNullException.ThrowIfNull(power);

        var owner = power.Owner;
        var runManager = RunManager.Instance;
        var ownerKind = TooltipOwnerKind.None;
        var ownerPlayerName = string.Empty;
        var ownerMonsterId = string.Empty;

        switch (owner)
        {
            case { IsEnemy: true, Monster: not null }:
                ownerKind = TooltipOwnerKind.MonsterModel;
                ownerMonsterId = owner.Monster.Id.Entry;
                break;
            case { IsPlayer: true, Player: not null }:
                ownerKind = TooltipOwnerKind.PlayerName;
                ownerPlayerName = PlatformUtil.GetPlayerName(runManager.NetService.Platform, owner.Player.NetId);
                break;
            case { IsPet: true, PetOwner: not null }:
                ownerKind = TooltipOwnerKind.PlayerName;
                ownerPlayerName = PlatformUtil.GetPlayerName(runManager.NetService.Platform, owner.PetOwner.NetId);
                break;
        }

        return new PowerTooltip
        {
            PowerIdStr = power.Id.Entry,
            Amount = power.Amount,
            OwnerKind = ownerKind,
            OwnerPlayerName = ownerPlayerName,
            OwnerMonsterIdStr = ownerMonsterId
        };
    }

    public static Color GetPowerColor(PowerModel power)
    {
        ArgumentNullException.ThrowIfNull(power);
        return power.AmountLabelColor;
    }

    public override string Render()
    {
        var power = ResolveModel();
        var ownerPrefix = BuildOwnerPrefix();
        var stackSuffix = Amount > 0 ? $" x{Amount}" : string.Empty;

        if (power is null)
            return $"{ownerPrefix}{PowerIdStr}{stackSuffix}";

        var color = GetPowerColor(power).ToHtml();
        var title = power.Title.GetFormattedText();
        var iconPrefix = $"[img={16}x{16}]{power.IconPath}[/img] ";

        return $"{ownerPrefix}{iconPrefix}[color={color}]{title}{stackSuffix}[/color]";
    }

    private string BuildOwnerPrefix()
    {
        var ownerText = ResolveOwnerText();
        if (string.IsNullOrWhiteSpace(ownerText))
            return string.Empty;

        var loc = new LocString("gameplay_ui", "LEMONSPIRE.chat.powerOwnerPrefix");
        loc.Add("Owner", ownerText);
        return loc.GetFormattedText();
    }

    private string ResolveOwnerText()
    {
        return OwnerKind switch
        {
            TooltipOwnerKind.PlayerName => OwnerPlayerName,
            TooltipOwnerKind.MonsterModel => ResolveMonsterTitle(OwnerMonsterIdStr),
            _ => string.Empty
        };
    }

    private static string ResolveMonsterTitle(string monsterId)
    {
        if (string.IsNullOrWhiteSpace(monsterId))
            return string.Empty;

        var model = StsUtil.ResolveModel<MonsterModel>(monsterId);
        return model?.Title.GetFormattedText() ?? monsterId;
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(PowerIdStr);
        writer.WriteInt(Amount);
        writer.WriteInt((int)OwnerKind);
        writer.WriteString(OwnerPlayerName);
        writer.WriteString(OwnerMonsterIdStr);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        PowerIdStr = reader.ReadString();
        Amount = reader.ReadInt();
        var ownerKind = reader.ReadInt();
        OwnerKind = Enum.IsDefined(typeof(TooltipOwnerKind), ownerKind)
            ? (TooltipOwnerKind)ownerKind
            : TooltipOwnerKind.None;
        OwnerPlayerName = reader.ReadString();
        OwnerMonsterIdStr = reader.ReadString();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        return model is null ? null : BuildHoverTipControl(model.DumbHoverTip, model.Icon);
    }

    private PowerModel? ResolveModel()
    {
        return StsUtil.ResolveModel<PowerModel>(PowerIdStr);
    }
}

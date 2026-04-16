using Godot;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class EnchantmentTooltip : Tooltip
{
    protected override string TypeTag => "enchant";

    public required string EnchantmentIdStr { get; set; }
    public int Amount { get; set; }

    public static EnchantmentTooltip FromModel(EnchantmentModel enchantment)
    {
        ArgumentNullException.ThrowIfNull(enchantment);
        return new EnchantmentTooltip
        {
            EnchantmentIdStr = enchantment.Id.Entry,
            Amount = enchantment.Amount
        };
    }

    public override string Render()
    {
        var enchantment = ResolveModel();
        if (enchantment is null) return "Broken Enchantment";

        var title = enchantment.Title.GetFormattedText();
        var iconPath = enchantment.IconPath;

        return $"[img={16}x{16}]{iconPath}[/img] {title}";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(EnchantmentIdStr);
        writer.WriteInt(Amount);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        EnchantmentIdStr = reader.ReadString();
        Amount = reader.ReadInt();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        return model is null ? null : BuildHoverTipControl(model.HoverTip, model.Icon);
    }

    private EnchantmentModel? ResolveModel()
    {
        return StsUtil.ResolveModel<EnchantmentModel>(EnchantmentIdStr);
    }
}

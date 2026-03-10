using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class PowerTooltip : Tooltip
{
    protected override string TypeTag => "power";

    public required string PowerIdStr { get; set; }
    public int Amount { get; set; }
    public bool IsPlayer { get; set; }
    public string ApplierName { get; set; } = "";

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(PowerIdStr);
        writer.WriteInt(Amount);
        writer.WriteBool(IsPlayer);
        writer.WriteString(ApplierName);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        PowerIdStr = reader.ReadString();
        Amount = reader.ReadInt();
        IsPlayer = reader.ReadBool();
        ApplierName = reader.ReadString();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        if (model is null) return null;

        var tip = model.DumbHoverTip;
        return BuildHoverTip(tip);
    }

    private PowerModel? ResolveModel()
    {
        // 从 ModelDb.AllPowers 查找 canonical instance
        foreach (var power in ModelDb.AllPowers)
            if (power.Id.Entry == PowerIdStr)
                return power;
        return null;
    }

    private static Control? BuildHoverTip(HoverTip tip)
    {
        try
        {
            var control = PreloadManager.Cache
                .GetScene("res://scenes/ui/hover_tip.tscn")
                .Instantiate<Control>();

            var title = control.GetNode<MegaLabel>("%Title");
            if (tip.Title is null)
                title.Visible = false;
            else
                title.SetTextAutoSize(tip.Title);

            control.GetNode<MegaRichTextLabel>("%Description").Text = tip.Description;
            control.GetNode<TextureRect>("%Icon").Texture = tip.Icon;

            if (tip.IsDebuff)
            {
                var bg = control.GetNode<CanvasItem>("%Bg");
                bg.Material = PreloadManager.Cache.GetMaterial("res://materials/ui/hover_tip_debuff.tres");
            }

            control.ResetSize();
            SetSubtreeMouseIgnore(control);
            return control;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Failed to build power hover tip: {ex.Message}");
            throw;
        }
    }

    private static void SetSubtreeMouseIgnore(Node node)
    {
        if (node is Control c)
            c.MouseFilter = Control.MouseFilterEnum.Ignore;

        foreach (var child in node.GetChildren())
            SetSubtreeMouseIgnore(child);
    }
}
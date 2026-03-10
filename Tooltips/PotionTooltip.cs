using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public sealed class PotionTooltip : Tooltip
{
    protected override string TypeTag => "potion";

    public required string ModelIdStr { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(ModelIdStr);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ModelIdStr = reader.ReadString();
    }

    public override Control? CreatePreview()
    {
        var model = ResolveModel();
        if (model is null) return null;

        return BuildHoverTip(model.HoverTip, model.Image);
    }

    private PotionModel? ResolveModel()
    {
        foreach (var potion in ModelDb.AllPotions)
            if (potion.Id.Entry == ModelIdStr)
                return potion;
        return null;
    }

    private static Control? BuildHoverTip(HoverTip tip, Texture2D? icon)
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
            
            // Use the potion image directly since HoverTip doesn't include it
            control.GetNode<TextureRect>("%Icon").Texture = icon;

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
            MainFile.Logger.Error($"Failed to build potion hover tip: {ex.Message}");
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
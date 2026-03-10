using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

/// <summary>
///     Tooltip with custom title and description.
///     Useful for displaying arbitrary rich text content.
/// </summary>
public sealed class RichTextTooltip : Tooltip
{
    protected override string TypeTag => "rt";

    public string? Title { get; set; }
    public required string Description { get; set; }
    public bool IsDebuff { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(Title ?? "");
        writer.WriteString(Description);
        writer.WriteBool(IsDebuff);
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var title = reader.ReadString();
        Title = string.IsNullOrEmpty(title) ? null : title;
        Description = reader.ReadString();
        IsDebuff = reader.ReadBool();
    }

    public override Control? CreatePreview()
    {
        return BuildHoverTip(Title, Description, IsDebuff);
    }

    private static Control? BuildHoverTip(string? title, string description, bool isDebuff)
    {
        try
        {
            var control = PreloadManager.Cache
                .GetScene("res://scenes/ui/hover_tip.tscn")
                .Instantiate<Control>();

            var titleLabel = control.GetNode<MegaLabel>("%Title");
            if (title is null)
                titleLabel.Visible = false;
            else
                titleLabel.SetTextAutoSize(title);

            control.GetNode<MegaRichTextLabel>("%Description").Text = description;
            control.GetNode<TextureRect>("%Icon").Texture = null;

            if (isDebuff)
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
            MainFile.Logger.Error($"Failed to build rich text hover tip: {ex.Message}");
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
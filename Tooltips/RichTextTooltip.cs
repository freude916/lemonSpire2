using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

/// <summary>
///     Tooltip with custom title and description.
///     Useful for displaying arbitrary rich text content.
/// </summary>
public sealed class RichTextTooltip : Tooltip
{
    private static readonly PropertyInfo? TitleProperty =
        typeof(HoverTip).GetProperty(nameof(HoverTip.Title));

    private static readonly PropertyInfo? DescriptionProperty =
        typeof(HoverTip).GetProperty(nameof(HoverTip.Description));

    protected override string TypeTag => "rt";

    public string? Title { get; set; }
    public required string Description { get; set; }
    public bool IsDebuff { get; set; }
    public string? IconPath { get; set; }

    public override string Render()
    {
        var iconPrefix = string.IsNullOrEmpty(IconPath) ? "" : $"[img={16}x{16}]{IconPath}[/img] ";
        return $"{iconPrefix}{Title}";
    }

    public override void Serialize(PacketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteString(Title ?? "");
        writer.WriteString(Description);
        writer.WriteBool(IsDebuff);
        writer.WriteString(IconPath ?? "");
    }

    public override void Deserialize(PacketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var title = reader.ReadString();
        Title = string.IsNullOrEmpty(title) ? null : title;
        Description = reader.ReadString();
        IsDebuff = reader.ReadBool();
        var iconPath = reader.ReadString();
        IconPath = string.IsNullOrEmpty(iconPath) ? null : iconPath;
    }

    public override Control? CreatePreview()
    {
        // Create HoverTip with reflection to set readonly properties
        var tip = new HoverTip
        {
            IsDebuff = IsDebuff,
            IsSmart = false,
            IsInstanced = false,
            Id = $"richtext:{Title ?? "untitled"}"
        };

        object boxed = tip;
        TitleProperty?.SetValue(boxed, Title);
        DescriptionProperty?.SetValue(boxed, Description);
        tip = (HoverTip)boxed;

        Texture2D? icon = null;
        if (!string.IsNullOrEmpty(IconPath) && ResourceLoader.Exists(IconPath))
            icon = ResourceLoader.Load<Texture2D>(IconPath);

        return BuildHoverTipControl(tip, icon);
    }
}

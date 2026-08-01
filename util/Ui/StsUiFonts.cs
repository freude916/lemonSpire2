using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;

namespace lemonSpire2.util.Ui;

/// <summary>
///     Applies the same font resources and locale substitutions used by the game's UI.
/// </summary>
public static class StsUiFonts
{
    private const string RegularFontPath = "res://themes/kreon_regular_shared.tres";
    private const string BoldFontPath = "res://themes/kreon_bold_shared.tres";
    private const string ItalicFontPath = "res://themes/bitter_medium_italic_glyph_space_one.tres";

    private static readonly StringName FontRoleMeta = "lemon_spire_font_role";
    private static readonly StringName Font = "font";
    private static readonly StringName NormalFont = "normal_font";
    private static readonly StringName BoldFont = "bold_font";
    private static readonly StringName ItalicsFont = "italics_font";

    private static Font? _regularFont;
    private static Font? _boldFont;
    private static Font? _italicFont;

    public static void Apply(Control control, FontType fontType = FontType.Regular)
    {
        ArgumentNullException.ThrowIfNull(control);

        control.SetMeta(FontRoleMeta, (int)fontType);

        if (control is RichTextLabel richTextLabel)
        {
            ApplyRichTextFonts(richTextLabel);
            return;
        }

        control.AddThemeFontOverride(Font, GetFont(fontType));
    }

    /// <summary>
    ///     Reapplies fonts to controls previously registered through <see cref="Apply"/>.
    /// </summary>
    public static void Refresh(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root is Control control && control.HasMeta(FontRoleMeta))
        {
            var fontType = (FontType)control.GetMeta(FontRoleMeta).AsInt32();
            Apply(control, fontType);
        }

        foreach (var child in root.GetChildren())
            Refresh(child);
    }

    private static void ApplyRichTextFonts(RichTextLabel label)
    {
        label.AddThemeFontOverride(NormalFont, GetFont(FontType.Regular));
        label.AddThemeFontOverride(BoldFont, GetFont(FontType.Bold));
        label.AddThemeFontOverride(ItalicsFont, GetFont(FontType.Italic));
    }

    private static Font GetFont(FontType fontType)
    {
        var locManager = LocManager.Instance;
        var substituteFont = locManager is null
            ? null
            : FontManager.GetSubstituteFont(locManager.Language, fontType);

        return substituteFont ?? GetDefaultFont(fontType);
    }

    private static Font GetDefaultFont(FontType fontType)
    {
        return fontType switch
        {
            FontType.Regular => _regularFont ??= LoadFont(RegularFontPath),
            FontType.Bold => _boldFont ??= LoadFont(BoldFontPath),
            FontType.Italic => _italicFont ??= LoadFont(ItalicFontPath),
            _ => throw new ArgumentOutOfRangeException(nameof(fontType), fontType, null)
        };
    }

    private static Font LoadFont(string path)
    {
        return ResourceLoader.Load<Font>(path, null, ResourceLoader.CacheMode.Reuse)
               ?? throw new InvalidOperationException($"Could not load game font resource: {path}");
    }
}

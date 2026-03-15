using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.Tooltips;

/// <summary>
///     Base class for serializable tooltips.
///     Implementations should provide CreatePreview() for UI rendering.
/// </summary>
public abstract class Tooltip
{
    private static int _nextRegistryId = 1;
    private static readonly Dictionary<int, WeakReference<Tooltip>> Registry = new();

    public static readonly int FontSize = 18;

    protected Tooltip()
    {
        RegistryId = _nextRegistryId++;
        Registry[RegistryId] = new WeakReference<Tooltip>(this);
    }

    internal static Logger Log { get; } = new("lemon.tooltip", LogType.Generic);

    protected abstract string TypeTag { get; }

    /// <summary>
    ///     Internal registry ID used for meta string lookup.
    /// </summary>
    public int RegistryId { get; }

    public abstract string Render();

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);

    /// <summary>
    ///     Creates a preview Control for this tooltip.
    ///     Returns null if preview cannot be created.
    /// </summary>
    public abstract Control? CreatePreview();

    /// <summary>
    ///     Converts this tooltip to an IHoverTip (optional, for NHoverTipSet compatibility).
    /// </summary>
    public virtual IHoverTip ToHoverTip()
    {
        throw new NotImplementedException();
    }

    public static Tooltip? TryResolve(int registryId)
    {
        if (!Registry.TryGetValue(registryId, out var weak))
        {
            Log.Debug($"TryResolve: registryId {registryId} not found");
            return null;
        }

        if (weak.TryGetTarget(out var tooltip))
            return tooltip;

        Log.Debug($"TryResolve: registryId {registryId} was garbage collected");
        Registry.Remove(registryId);
        return null;
    }

    public static void Cleanup()
    {
        var deadKeys = new List<int>();
        foreach (var (id, weak) in Registry)
            if (!weak.TryGetTarget(out _))
                deadKeys.Add(id);
        foreach (var id in deadKeys)
            Registry.Remove(id);
    }

    public string ToMetaString()
    {
        return $"{TypeTag}:{RegistryId}";
    }

    public static Tooltip? FromMetaString(string meta)
    {
        var span = meta.AsSpan();
        var colonIndex = span.IndexOf(':');
        if (colonIndex < 0)
        {
            Log.Debug($"FromMetaString: invalid meta format (no colon): {meta}");
            return null;
        }

        var idSpan = span[(colonIndex + 1)..];
        if (!int.TryParse(idSpan, out var id))
        {
            Log.Debug($"FromMetaString: failed to parse id from: {meta}");
            return null;
        }

        return TryResolve(id);
    }

    protected static Control? BuildHoverTipControl(HoverTip tip, Texture2D? icon = null)
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

    protected static void SetSubtreeMouseIgnore(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node is Control c)
            c.MouseFilter = Control.MouseFilterEnum.Ignore;

        foreach (var child in node.GetChildren())
            SetSubtreeMouseIgnore(child);
    }
}

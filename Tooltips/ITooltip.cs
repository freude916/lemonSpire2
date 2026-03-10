using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Tooltips;

public abstract class Tooltip : IMeta
{
    private static int _nextId = 1;
    private static readonly Dictionary<int, WeakReference<Tooltip>> Registry = new();

    protected Tooltip()
    {
        Id = _nextId++;
        Registry[Id] = new WeakReference<Tooltip>(this);
    }

    protected abstract string TypeTag { get; }

    public int Id { get; }

    public abstract void Serialize(PacketWriter writer);
    public abstract void Deserialize(PacketReader reader);

    public static Tooltip? TryResolve(int id)
    {
        if (!Registry.TryGetValue(id, out var weak)) return null;

        if (weak.TryGetTarget(out var tooltip))
            return tooltip;

        // 懒清理：目标已被 GC
        Registry.Remove(id);
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
        return $"{TypeTag}:{Id}";
    }

    /// <summary>
    ///     Parses a meta string and resolves the Tooltip from the weak reference registry.
    ///     Format: "tag:id" (tag is ignored, only id is used)
    /// </summary>
    public static Tooltip? FromMetaString(string meta)
    {
        var span = meta.AsSpan();
        var colonIndex = span.IndexOf(':');
        if (colonIndex < 0) return null;

        var idSpan = span[(colonIndex + 1)..];
        if (!int.TryParse(idSpan, out var id)) return null;

        return TryResolve(id);
    }

    public abstract Control? CreatePreview();
}
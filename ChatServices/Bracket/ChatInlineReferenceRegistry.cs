using lemonSpire2.Chat.Input.Abstractions;

namespace lemonSpire2.Chat.Input.Registry;

public sealed class ChatInlineReferenceRegistry
{
    private readonly Dictionary<string, IChatInlineReference> _types = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<IChatInlineReference> All =>
        _types.Values.OrderBy(type => type.TypeName, StringComparer.OrdinalIgnoreCase);

    public void Register(IChatInlineReference type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_types.ContainsKey(type.TypeName))
            throw new InvalidOperationException($"Duplicate inline reference type '{type.TypeName}'.");
        _types[type.TypeName] = type;
    }

    public bool TryGet(string typeName, out IChatInlineReference? type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        return _types.TryGetValue(typeName, out type);
    }
}

using lemonSpire2.Chat.Input.Abstractions;

namespace lemonSpire2.Chat.Input.Registry;

public sealed class ChatInlineReferenceRegistry
{
    private readonly Dictionary<string, IChatInlineReferenceType> _types = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<IChatInlineReferenceType> All =>
        _types.Values.OrderBy(type => type.TypeName, StringComparer.OrdinalIgnoreCase);

    public void Register(IChatInlineReferenceType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _types[type.TypeName] = type;
    }

    public bool TryGet(string typeName, out IChatInlineReferenceType? type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        return _types.TryGetValue(typeName, out type);
    }
}

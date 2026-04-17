using System.Buffers;
using lemonSpire2.Chat.Input.Abstractions;
using lemonSpire2.Chat.Message;

namespace lemonSpire2.Chat.Input.Registry;

public sealed class ChatSubmitTokenHandlerRegistry
{
    private readonly Dictionary<char, IChatSubmitTokenHandler> _handlers = new();
    public int Count => _handlers.Count;

    public void Register(IChatSubmitTokenHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (_handlers.ContainsKey(handler.TriggerChar))
            throw new InvalidOperationException($"Duplicate submit token handler '{handler.TriggerChar}'.");
        _handlers[handler.TriggerChar] = handler;
    }

    public bool TryGet(char triggerChar, out IChatSubmitTokenHandler? handler)
    {
        return _handlers.TryGetValue(triggerChar, out handler);
    }

    public SearchValues<char> CreateSearchValues()
    {
        return SearchValues.Create([.. _handlers.Keys]);
    }

    public bool TryParse(string text, int startIndex, out IMsgSegment segment, out int length)
    {
        ArgumentNullException.ThrowIfNull(text);
        segment = null!;
        length = 0;

        if (startIndex < 0 || startIndex >= text.Length)
            return false;

        return TryGet(text[startIndex], out var handler) &&
               handler is not null &&
               handler.TryParse(text, startIndex, out segment, out length);
    }
}

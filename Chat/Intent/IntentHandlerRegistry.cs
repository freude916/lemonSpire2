using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.Chat.Intent;

public class IntentHandlerRegistry
{
    private readonly Dictionary<Type, Action<IIntent>> _handlers = new();
    private static Logger Log => ChatUiPatch.Log;

    public void Register<T>(Action<T> handler) where T : IIntent
    {
        Log.Info($"Registering handler for {typeof(T)}");
        _handlers[typeof(T)] = intent => handler((T)intent);
    }

    public bool TryHandle(IIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (_handlers.TryGetValue(intent.GetType(), out var handler))
        {
            Log.Info($"Handling handler for {intent.GetType()}");
            handler(intent);
            return true;
        }

        Log.Debug($"No handler registered for {intent.GetType()}");
        return false;
    }
}

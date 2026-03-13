namespace lemonSpire2.Chat.Intent;

public class IntentHandlerRegistry
{
    private readonly Dictionary<Type, Action<IIntent>> _handlers = new();

    public void Register<T>(Action<T> handler) where T : IIntent
    {
        MainFile.Logger.Info($"Registering handler for {typeof(T)}");
        _handlers[typeof(T)] = intent => handler((T)intent);
    }

    public bool TryHandle(IIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (_handlers.TryGetValue(intent.GetType(), out var handler))
        {
            MainFile.Logger.Info($"Handling handler for {intent.GetType()}");
            handler(intent);
            return true;
        }

        return false;
    }
}

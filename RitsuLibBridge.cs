using STS2RitsuLib.Settings;

namespace lemonSpire2;

internal static class RitsuLibBridge
{
    private static readonly Lock Gate = new();
    private static bool _isRegistered;

    public static bool IsAvailable => _isRegistered;

    public static void Initialize()
    {
        lock (Gate)
        {
            if (_isRegistered)
                return;

            ModSettingsRuntimeReflectionInteropMirror.RegisterProviderType(typeof(LemonSpireConfig));
            _isRegistered = true;
        }
    }

    public static void Disable()
    {
        lock (Gate)
        {
            _isRegistered = false;
        }
    }
}

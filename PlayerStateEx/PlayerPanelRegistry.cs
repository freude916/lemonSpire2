using lemonSpire2.PlayerStateEx.Panel;

namespace lemonSpire2.PlayerStateEx;

/// <summary>
///     玩家悬浮面板提供者注册表
///     管理所有 IPlayerPanelProvider 的注册和获取
/// </summary>
public static class PlayerPanelRegistry
{
    private static readonly List<IPlayerPanelProvider> Providers = new();
    private static bool _initialized;

    /// <summary>
    ///     初始化注册表，注册内置提供者
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // 注册内置提供者
        Register(new HandCardProvider());
        Register(new PotionProvider());

        MainFile.Logger.Info($"PlayerPanelRegistry initialized with {Providers.Count} providers");
    }

    /// <summary>
    ///     注册提供者
    /// </summary>
    public static void Register(IPlayerPanelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (Providers.Any(p => p.ProviderId == provider.ProviderId))
        {
            MainFile.Logger.Warn($"Provider {provider.ProviderId} already registered, skipping");
            return;
        }

        Providers.Add(provider);
        // 按 Priority 排序
        Providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        MainFile.Logger.Debug($"Registered player panel provider: {provider.ProviderId}");
    }

    /// <summary>
    ///     获取所有提供者（已按 Priority 排序）
    /// </summary>
    public static IEnumerable<IPlayerPanelProvider> GetProviders()
    {
        if (!_initialized) Initialize();
        return Providers;
    }

    /// <summary>
    ///     获取指定 ID 的提供者
    /// </summary>
    public static IPlayerPanelProvider? GetProvider(string providerId)
    {
        return Providers.FirstOrDefault(p => p.ProviderId == providerId);
    }

    /// <summary>
    ///     清除所有注册
    /// </summary>
    public static void Clear()
    {
        Providers.Clear();
        _initialized = false;
    }
}

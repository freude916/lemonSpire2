using lemonSpire2.PlayerStateEx.PanelProvider;
using lemonSpire2.util;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.PlayerStateEx;

/// <summary>
///     玩家悬浮面板提供者注册表
///     管理所有 IPlayerPanelProvider 的注册和获取
/// </summary>
public static class PlayerPanelRegistry
{
    private static readonly PriorityRegistry<IPlayerPanelProvider> Registry = new();
    private static bool _initialized;
    internal static Logger Log { get; } = new("lemon.player", LogType.Generic);

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
        Register(new ShopProvider());
        Register(new CardRewardProvider());

        Log.Info($"PlayerPanelRegistry initialized with {Registry.Items.Count} providers");
    }

    /// <summary>
    ///     注册提供者
    /// </summary>
    public static void Register(IPlayerPanelProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (Registry.Items.Any(p => p.ProviderId == provider.ProviderId))
        {
            Log.Warn($"Provider {provider.ProviderId} already registered, skipping");
            return;
        }

        Registry.Register(provider, p => p.Priority, p => p.ProviderId);
        Log.Debug($"Registered player panel provider: {provider.ProviderId}");
    }

    /// <summary>
    ///     获取所有提供者（已按 Priority 排序）
    /// </summary>
    public static IEnumerable<IPlayerPanelProvider> GetProviders()
    {
        if (!_initialized) Initialize();
        return Registry.Items;
    }

    /// <summary>
    ///     获取指定 ID 的提供者
    /// </summary>
    public static IPlayerPanelProvider? GetProvider(string providerId)
    {
        return Registry.Items.FirstOrDefault(p => p.ProviderId == providerId);
    }

    /// <summary>
    ///     清除所有注册
    /// </summary>
    public static void Clear()
    {
        Registry.Clear();
        _initialized = false;
    }
}

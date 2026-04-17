using lemonSpire2.util;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;

namespace lemonSpire2.PlayerStateEx;

/// <summary>
///     Registry for tooltip providers that contribute to multiplayer player state tooltips.
///     Providers can be registered/unregistered dynamically.
/// </summary>
public static class PlayerTooltipRegistry
{
    private static readonly PriorityRegistry<ITooltipProvider> Registry = new();

    /// <summary>
    ///     Check if there are any providers registered.
    /// </summary>
    public static bool HasProviders => Registry.HasItems;

    /// <summary>
    ///     Register a tooltip provider.
    /// </summary>
    public static void Register(ITooltipProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        Registry.Register(provider, p => p.Priority, p => p.Id);
    }

    /// <summary>
    ///     Unregister a tooltip provider by its ID.
    /// </summary>
    public static void Unregister(string providerId)
    {
        Registry.UnregisterById(p => p.Id, providerId);
    }

    /// <summary>
    ///     Unregister a tooltip provider instance.
    /// </summary>
    public static void Unregister(ITooltipProvider provider)
    {
        Registry.Unregister(provider);
    }

    /// <summary>
    ///     Get all hover tips for a player from registered providers.
    /// </summary>
    public static IEnumerable<IHoverTip> GetHoverTips(Player player)
    {
        foreach (var provider in Registry.Items)
        {
            if (!provider.ShouldShow(player)) continue;

            var tip = provider.CreateHoverTip(player);
            if (tip.HasValue) yield return tip.Value;
        }
    }

    /// <summary>
    ///     Clear all registered providers.
    /// </summary>
    public static void Clear()
    {
        Registry.Clear();
    }
}

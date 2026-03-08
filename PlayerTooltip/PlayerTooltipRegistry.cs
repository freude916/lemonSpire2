using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;

namespace lemonSpire2.PlayerTooltip;

/// <summary>
///     Registry for tooltip providers that contribute to multiplayer player state tooltips.
///     Providers can be registered/unregistered dynamically.
/// </summary>
public static class PlayerTooltipRegistry
{
    private static readonly List<ITooltipProvider> _providers = new();

    /// <summary>
    ///     Check if there are any providers registered.
    /// </summary>
    public static bool HasProviders => _providers.Count > 0;

    /// <summary>
    ///     Register a tooltip provider.
    /// </summary>
    public static void Register(ITooltipProvider provider)
    {
        if (_providers.Contains(provider))
        {
            return;
        }

        _providers.Add(provider);
        _providers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    /// <summary>
    ///     Unregister a tooltip provider by its ID.
    /// </summary>
    public static void Unregister(string providerId) => _providers.RemoveAll(p => p.Id == providerId);

    /// <summary>
    ///     Unregister a tooltip provider instance.
    /// </summary>
    public static void Unregister(ITooltipProvider provider) => _providers.Remove(provider);

    /// <summary>
    ///     Get all hover tips for a player from registered providers.
    /// </summary>
    public static IEnumerable<IHoverTip> GetHoverTips(Player player)
    {
        foreach (var provider in _providers)
        {
            if (!provider.ShouldShow(player))
            {
                continue;
            }

            var tip = provider.CreateHoverTip(player);
            if (tip.HasValue)
            {
                yield return tip.Value;
            }
        }
    }

    /// <summary>
    ///     Clear all registered providers.
    /// </summary>
    public static void Clear() => _providers.Clear();
}
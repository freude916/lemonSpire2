using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;

namespace lemonSpire2.PlayerTooltip;

/// <summary>
///     Provides tooltip content for multiplayer player state display.
///     Implementations can register with <see cref="PlayerTooltipRegistry" /> to contribute tooltips.
/// </summary>
public interface ITooltipProvider
{
    /// <summary>
    ///     Unique identifier for this provider. Used for deduplication.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     Display priority. Lower values appear first (top). Default is 100.
    /// </summary>
    int Priority => 100;

    /// <summary>
    ///     Whether this provider currently has content to display for the given player.
    ///     Return false to skip this provider.
    /// </summary>
    bool ShouldShow(Player player) => true;

    /// <summary>
    ///     Create the hover tip content for the given player.
    ///     Return null if no content should be displayed.
    /// </summary>
    HoverTip? CreateHoverTip(Player player);
}
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace lemonSpire2.PlayerStateEx.OverlayPanel;

internal static class OverlayInteractionGuard
{
    public static bool IsBlockedByTargetSelection(Node? context = null)
    {
        var targetManager = NTargetManager.Instance;
        if (targetManager == null) return false;
        if (targetManager.IsInSelection) return true;

        if (context?.GetTree() == null) return false;
        return targetManager.LastTargetingFinishedFrame == context.GetTree().GetFrame();
    }
}

using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace lemonSpire2.SendGameItem;

[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGame), "_ExitTree")]
public static class ItemInputCaptureCleanupPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        SendItemInputPatch.Captures.ForEachLive(c => c.QueueFree());
        SendItemInputPatch.Log.Debug("ItemInputCapture instances cleaned up");
    }
}

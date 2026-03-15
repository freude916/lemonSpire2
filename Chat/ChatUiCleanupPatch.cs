using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace lemonSpire2.Chat;

[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGame), "_ExitTree")]
public static class ChatUiCleanupPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        ChatStore.Instance = null;
        ChatUiPatch.ChatUIs.ForEachLive(ui => ui.QueueFree());

        ChatUiPatch.Log.Debug("ChatUI instances cleaned up");
    }
}

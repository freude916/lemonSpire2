using HarmonyLib;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.SendItem;

/// <summary>
///     注入 ItemInputCapture 到场景树
/// </summary>
[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class SendItemInputPatch
{
    internal static readonly WeakNodeRegistry<ItemInputCapture> Captures = new();

    [HarmonyPostfix]
    public static void Postfix(NGlobalUi __instance, RunState runState)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer())
            return;

        var capture = new ItemInputCapture
        {
            Name = "ItemInputCapture"
        };

        __instance.AddChild(capture);
        Captures.Register(capture);
        MainFile.Logger.Info("ItemInputCapture injected");
    }
}

[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGame), "_ExitTree")]
public static class ItemInputCaptureCleanupPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        SendItemInputPatch.Captures.ForEachLive(c => c.QueueFree());
        MainFile.Logger.Debug("ItemInputCapture instances cleaned up");
    }
}

using HarmonyLib;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;
using LogType = MegaCrit.Sts2.Core.Logging.LogType;

namespace lemonSpire2.SendGameItem;

/// <summary>
///     注入 ItemInputCapture 到场景树
/// </summary>
[HarmonyPatchCategory("Chat")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class SendItemInputPatch
{
    internal static readonly WeakNodeRegistry<ItemInputCapture> Captures = new();
    internal static Logger Log { get; } = new("lemon.item", LogType.Network);

    [HarmonyPostfix]
    public static void Postfix(NGlobalUi __instance, RunState runState)
    {
        ArgumentNullException.ThrowIfNull(__instance);
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer())
        {
            Log.Debug("Not multiplayer, skipping ItemInputCapture injection");
            return;
        }

        var capture = new ItemInputCapture
        {
            Name = "ItemInputCapture"
        };

        __instance.AddChild(capture);
        Captures.Register(capture);
        Log.Info("ItemInputCapture injected");
    }
}

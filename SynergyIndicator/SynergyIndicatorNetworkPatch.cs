using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.SynergyIndicator;

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class SynergyIndicatorNetworkPatch
{
    private static Logger Log => SynergyIndicatorPatch.Log;

    [HarmonyPostfix]
    public static void Postfix(NGlobalUi __instance, RunState runState)
    {
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        IndicatorManager.Instance.InitializeNetwork(netService);
        Log.Info("IndicatorManager network initialized");
    }
}
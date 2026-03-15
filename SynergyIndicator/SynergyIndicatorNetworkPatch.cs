using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.SynergyIndicator;

[HarmonyPatchCategory("HandshakeIndicator")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class SynergyIndicatorNetworkPatch
{
    [HarmonyPostfix]
    public static void Postfix(NGlobalUi __instance, RunState runState)
    {
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        IndicatorManager.Instance.InitializeNetwork(netService);
        Log.Info("IndicatorManager network initialized");
    }
}

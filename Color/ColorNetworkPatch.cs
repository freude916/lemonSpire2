using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.PlayerColor;

/// <summary>
///     Color 模块网络初始化 Patch
///     在 NGlobalUi.Initialize 时初始化网络处理器
/// </summary>
[HarmonyPatchCategory("PlayerColor")]
[HarmonyPatch(typeof(NGlobalUi), "Initialize")]
public static class ColorNetworkPatch
{
    public static ColorNetworkHandler? NetworkHandler { get; private set; }

    [HarmonyPostfix]
    public static void Postfix(NGlobalUi __instance, RunState runState)
    {
        var netService = RunManager.Instance.NetService;
        if (!netService.Type.IsMultiplayer()) return;

        NetworkHandler = new ColorNetworkHandler(netService);
        ColorManager.Log.Info("ColorManager network initialized");
    }
}

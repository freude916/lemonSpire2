using Godot;
using HarmonyLib;
using lemonSpire2.ColorEx.Message;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.ColorEx;

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

        // TEST: 预设测试颜色
        // InitTestColors();
    }

    private static void InitTestColors()
    {
        // netId 1 = 红色
        ColorManager.Instance.SetPlayerColor(1, Colors.Red);
        // netId 1000 = 蓝色
        ColorManager.Instance.SetPlayerColor(1000, Colors.Blue);
        ColorManager.Log.Info("Test colors initialized: netId 1=red, netId 1000=blue");
    }
}

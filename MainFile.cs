using BaseLib.Config;
using Godot;
using HarmonyLib;
using lemonSpire2.Chat;
using lemonSpire2.ColorEx;
using lemonSpire2.PlayerStateEx;
using lemonSpire2.SendGameItem;
using lemonSpire2.StatsTracker;
using lemonSpire2.SyncReward;
using lemonSpire2.SyncShop;
using lemonSpire2.SynergyIndicator;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    internal const string ModId = "lemonSpire2";

    public static Logger Log { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        ModConfigRegistry.Register(ModId, new LemonSpireConfig());
        ModSoundManager.Initialize();

        // 设置日志级别为 Debug，启用所有模块的调试日志
        SetupLogLevels();

        Harmony harmony = new(ModId);

        if (LemonSpireConfig.EnableQoL)
            harmony.CreateClassProcessor(typeof(NMultiplayerPlayerExpandedStatePatch)).Patch();

        if (LemonSpireConfig.EnableChat)
        {
            harmony.CreateClassProcessor(typeof(ChatUiPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ChatUiCleanupPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SendItemInputPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ItemInputCaptureCleanupPatch)).Patch();
        }

        if (LemonSpireConfig.EnableSynergyIndicator)
        {
            harmony.CreateClassProcessor(typeof(SynergyIndicatorPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SynergyIndicatorNetworkPatch)).Patch();
        }

        if (LemonSpireConfig.EnableStatsTracker)
        {
            harmony.CreateClassProcessor(typeof(PowerCmdPatch)).Patch();
            StatsTrackerManager.Instance.Initialize();
            PlayerTooltipRegistry.Register(new StatsTooltipProvider());
        }

        if (LemonSpireConfig.EnableSync)
        {
            harmony.CreateClassProcessor(typeof(ShopNetworkInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ShopRoomPatch)).Patch();
            harmony.CreateClassProcessor(typeof(CardRewardNetworkInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RewardsScreenPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RunManagerPatch)).Patch();
        }

        if (LemonSpireConfig.EnablePlayerColor)
        {
            harmony.CreateClassProcessor(typeof(PlayerNameColorPatch)).Patch();
            harmony.CreateClassProcessor(typeof(MapDrawColorPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RemoteCursorColorPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ColorNetworkPatch)).Patch();
            harmony.CreateClassProcessor(typeof(PlayerColorButtonPatch)).Patch();
        }

        if (PlayerTooltipRegistry.HasProviders)
            harmony.CreateClassProcessor(typeof(NMultiplayerPlayerStatePatch)).Patch();

        Log.Info("lemonSpire2 mod initialized");
    }

    private static void SetupLogLevels()
    {
    }
}

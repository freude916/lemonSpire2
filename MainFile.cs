using Godot;
using HarmonyLib;
using lemonSpire2.Chat;
using lemonSpire2.PlayerStateEx;
using lemonSpire2.SendGameItem;
using lemonSpire2.StatsTracker;
using lemonSpire2.SyncReward;
using lemonSpire2.SyncShop;
using lemonSpire2.SynergyIndicator;
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
        // 设置日志级别为 Debug，启用所有模块的调试日志
        SetupLogLevels();

        Harmony harmony = new(ModId);

        if (EnableQoL) harmony.CreateClassProcessor(typeof(NMultiplayerPlayerExpandedStatePatch)).Patch();

        if (EnableChat)
        {
            harmony.CreateClassProcessor(typeof(ChatUiPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ChatUiCleanupPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SendItemInputPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ItemInputCaptureCleanupPatch)).Patch();
        }

        if (EnableSynergyIndicator)
        {
            harmony.CreateClassProcessor(typeof(SynergyIndicatorPatch)).Patch();
            harmony.CreateClassProcessor(typeof(SynergyIndicatorNetworkPatch)).Patch();
        }

        if (EnableStatsTracker)
        {
            harmony.CreateClassProcessor(typeof(PowerCmdPatch)).Patch();
            StatsTrackerManager.Instance.Initialize();
            PlayerTooltipRegistry.Register(new StatsTooltipProvider());
        }

        if (EnableSync)
        {
            harmony.CreateClassProcessor(typeof(ShopNetworkInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ShopRoomPatch)).Patch();
            harmony.CreateClassProcessor(typeof(CardRewardNetworkInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RewardsScreenPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RunManagerPatch)).Patch();
        }

        if (PlayerTooltipRegistry.HasProviders)
            harmony.CreateClassProcessor(typeof(NMultiplayerPlayerStatePatch)).Patch();

        Log.Info("lemonSpire2 mod initialized");
    }

    private static void SetupLogLevels()
    {
        // 为所有 LogType 设置 Debug 级别，启用调试日志
        if (false)
        {
            Logger.SetLogLevelForType(LogType.Generic, LogLevel.Debug);
            Logger.SetLogLevelForType(LogType.Network, LogLevel.Debug);
            Logger.SetLogLevelForType(LogType.Actions, LogLevel.Debug);
            Logger.SetLogLevelForType(LogType.GameSync, LogLevel.Debug);
            Logger.SetLogLevelForType(LogType.VisualSync, LogLevel.Debug);
        }
    }

    #region Feature Flags

    /// <summary>
    ///     Multiplayer QoL System:
    /// </summary>
    public static bool EnableQoL { get; set; } = true;

    /// <summary> Multiplayer Chat System</summary>
    public static bool EnableChat { get; set; } = true;


    /// <summary> Allies Support Indicator </summary>
    public static bool EnableSynergyIndicator { get; set; } = true;

    /// <summary> Stastics Tracker </summary>
    public static bool EnableStatsTracker { get; set; } = true;

    /// <summary> Extra Sync </summary>
    public static bool EnableSync { get; set; } = true;

    #endregion
}

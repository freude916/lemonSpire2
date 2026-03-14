using Godot;
using HarmonyLib;
using lemonSpire2.Chat;
using lemonSpire2.PlayerStateEx;
using lemonSpire2.PlayerStateEx.RewardEx;
using lemonSpire2.PlayerStateEx.ShopEx;
using lemonSpire2.SendGameItem;
using lemonSpire2.StatsTracker;
using lemonSpire2.SynergyIndicator;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    internal const string ModId = "lemonSpire2";

    public static Logger Logger { get; } =
        new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

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

        if (EnableShopSync)
        {
            harmony.CreateClassProcessor(typeof(ShopNetworkInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(ShopRoomPatch)).Patch();
        }

        if (EnableCardRewardSync)
        {
            harmony.CreateClassProcessor(typeof(CardRewardNetworkInitPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RewardsScreenPatch)).Patch();
            harmony.CreateClassProcessor(typeof(RunManagerPatch)).Patch();
        }

        if (PlayerTooltipRegistry.HasProviders)
            harmony.CreateClassProcessor(typeof(NMultiplayerPlayerStatePatch)).Patch();

        Logger.Info("lemonSpire2 mod initialized");
    }

    #region Feature Flags

    /// <summary> Multiplayer Chat System</summary>
    public static bool EnableChat { get; set; } = true;


    /// <summary> Allies Support Indicator </summary>
    public static bool EnableSynergyIndicator { get; set; } = true;

    /// <summary> Stastics Tracker </summary>
    public static bool EnableStatsTracker { get; set; } = true;

    /// <summary> Shop Inventory Sync </summary>
    public static bool EnableShopSync { get; set; } = true;

    /// <summary> Card Reward Sync </summary>
    public static bool EnableCardRewardSync { get; set; } = true;

    #endregion
}

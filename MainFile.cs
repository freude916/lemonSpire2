using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using lemonSpire2.DamageTracker;

namespace lemonSpire2;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    internal const string ModId = "lemonSpire2";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        // Initialize DamageTracker
        DamageTrackerManager.Instance.Initialize();

        Logger.Info("lemonSpire2 mod initialized");
    }
}
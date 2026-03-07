using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace lemonSpire2.QuickRestart;

public static class QuickSaveLoad
{
    private const string RetryButtonName = "RetryButton";
    private static NPauseMenuButton? _retryButton;

    [HarmonyPatch(typeof(NPauseMenu), "_Ready")]
    public static class PauseMenuReadyPatch
    {
        [HarmonyPostfix]
        public static void Postfix(
            Godot.Control ____buttonContainer,
            NPauseMenuButton ____saveAndQuitButton,
            NPauseMenuButton ____giveUpButton)
        {
            if (_retryButton != null && GodotObject.IsInstanceValid(_retryButton))
                return;

            _retryButton = PauseMenuPatch.InjectButton(
                ____buttonContainer,
                ____saveAndQuitButton,
                ____giveUpButton,
                ModLocalization.Get("pause_menu.retry", "Retry"),
                _ => QuickLoad(),
                RetryButtonName
            );

            if (_retryButton != null && !SaveManager.Instance.HasRunSave)
                _retryButton.Disable();
        }
    }

    public static void QuickLoad()
    {
        TaskHelper.RunSafely(QuickLoadAsync());
    }

    private static async Task QuickLoadAsync()
    {
        try
        {
            var result = SaveManager.Instance.LoadRunSave();
            if (!result.Success || result.SaveData == null)
            {
                MainFile.Logger.Error($"Quick Load failed: could not read autosave. Status={result.Status}");
                return;
            }

            var serializableRun = result.SaveData;
            var runState = RunState.FromSerializable(serializableRun);

            RunManager.Instance.ActionQueueSet.Reset();
            NRunMusicController.Instance?.StopMusic();

            await NGame.Instance!.Transition.FadeOut();
            RunManager.Instance.CleanUp();

            RunManager.Instance.SetUpSavedSinglePlayer(runState, serializableRun);
            NGame.Instance.ReactionContainer.InitializeNetworking(new NetSingleplayerGameService());
            await NGame.Instance.LoadRun(runState, serializableRun.PreFinishedRoom);
            await NGame.Instance.Transition.FadeIn();

            _retryButton = null;
            MainFile.Logger.Info("Quick Load completed successfully.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Quick Load failed: {ex}");
        }
    }
}
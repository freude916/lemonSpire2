using Godot;
using lemonSpire2.PlayerStateEx.RemoteFlash;
using lemonSpire2.util;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     药水显示提供者
///     显示玩家的药水栏
///     支持鼠标点击：左键闪烁、Alt+Click 发送药水到聊天
/// </summary>
public class PotionProvider : IPlayerPanelProvider
{
    private const float PotionScale = 0.6f;
    private static Logger Log => PlayerPanelRegistry.Log;

    #region Event Handlers

    private static void OnPotionHolderGuiInput(Player player, NPotionHolder holder, PotionModel potion,
        InputEvent @event)
    {
        if (StsUtil.IsInSelection(holder))
            return;

        switch (@event)
        {
            case InputEventMouseButton
            {
                Pressed: true, AltPressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right
            }:
                Log.Debug(
                    $"PotionHolder alt-clicked: {potion.Id.Entry}, Button={((InputEventMouseButton)@event).ButtonIndex}");
                PlayerPanelChatHelper.SendPotionToChat(player, "LEMONSPIRE.chat.potionShare", potion);
                holder.GetViewport()?.SetInputAsHandled();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }:
                Log.Debug($"PotionHolder left-clicked: {potion.Id.Entry}");
                PlayerPanelChatHelper.RequestRemoteFlash(player, RemoteUiFlashKind.Potion, potion);
                holder.GetViewport()?.SetInputAsHandled();
                break;
        }
    }

    #endregion

    #region Helpers

    private static Action SubscribePotionEvents(Player player, Action onUpdate, bool shouldLog)
    {
        // 事件类型是 Action<PotionModel>，需要适配
        void OnPotionChanged(PotionModel potion)
        {
            if (shouldLog)
                Log.Debug($"Potion event triggered for {potion?.Id.Entry ?? "null"}");
            onUpdate();
        }

        player.PotionProcured += OnPotionChanged;
        player.PotionDiscarded += OnPotionChanged;
        player.UsedPotionRemoved += OnPotionChanged;

        return () =>
        {
            player.PotionProcured -= OnPotionChanged;
            player.PotionDiscarded -= OnPotionChanged;
            player.UsedPotionRemoved -= OnPotionChanged;
        };
    }

    #endregion

    #region IPlayerPanelProvider Implementation

    public string Id => "potions";
    public int Priority => 20;
    public string DisplayName => new LocString("gameplay_ui", "LEMONSPIRE.panel.potions").GetFormattedText();

    public bool ShouldShow(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        // 只显示有药水的玩家
        return player.Potions.Any();
        // return true; // 即使没有药水也显示，保持界面一致性
    }

    public Control CreateContent(Player player)
    {
        var container = new HBoxContainer
        {
            Name = "PotionsContainer",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        container.AddThemeConstantOverride("separation", 4);

        // 不在这里调用 UpdateContent，等待加入场景树后再调用
        return container;
    }

    public void UpdateContent(Player player, Control content)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(content);
        if (content is not HBoxContainer container) return;

        // 清除现有内容
        UiUtils.ClearChildren(container);

        Log.Debug($"Updating content, player has {player.Potions.Count()} potions");

        foreach (var potion in player.Potions)
        {
            Log.Debug($"Creating NPotion for {potion.Id.Entry}");

            var nPotion = NPotion.Create(potion);
            if (nPotion == null)
            {
                Log.Warn($"NPotion.Create returned null for {potion.Id.Entry}");
                continue;
            }

            var holder = NPotionHolder.Create(false);

            holder.GuiInput += @event => OnPotionHolderGuiInput(player, holder, potion, @event);

            container.AddChild(holder);
            holder.AddPotion(nPotion);
            nPotion.Position = Vector2.Zero; // 关键：重置位置，否则会出现偏移
            UiUtils.SetPotionScale(holder, PotionScale);
            nPotion.Scale = Vector2.One * PotionScale;
            // Use the larger of current Size and minimum size to avoid one-frame lag and zero-size edge cases.
            var potionSize = nPotion.Size;
            var potionMinSize = nPotion.GetMinimumSize();
            var baseSize = new Vector2(
                Mathf.Max(potionSize.X, potionMinSize.X),
                Mathf.Max(potionSize.Y, potionMinSize.Y)
            );
            if (baseSize.X <= 0f || baseSize.Y <= 0f)
                baseSize = new Vector2(48f, 48f);

            holder.CustomMinimumSize = baseSize * PotionScale;

            Log.Debug($"Added potion {potion.Id.Entry} to holder, MouseFilter={holder.MouseFilter}");
        }
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);
        Log.Debug($"SubscribeEvents for player with {player.Potions.Count()} potions");

        return SubscribePotionEvents(player, onUpdate, true);
    }

    public Action SubscribeVisibilityEvents(Player player, Action onVisibilityChanged)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onVisibilityChanged);
        return SubscribePotionEvents(player, onVisibilityChanged, false);
    }

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        UiUtils.ClearChildren(content);
    }

    #endregion
}

using Godot;
using lemonSpire2.Chat.Message;
using lemonSpire2.Tooltips;
using lemonSpire2.util.Ui;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace lemonSpire2.PlayerStateEx.PanelProvider;

/// <summary>
///     药水显示提供者
///     显示玩家的药水栏
///     支持 Alt+Click 发送药水到聊天
/// </summary>
public class PotionProvider : IPlayerPanelProvider
{
    private const float PotionScale = 0.6f;
    private static Logger Log => PlayerPanelRegistry.Log;

    #region Event Handlers

    private static void OnPotionHolderReleased(NPotionHolder holder, PotionModel potion)
    {
        Log.Debug($"PotionHolder released: {potion.Id.Entry}, Alt={Input.IsKeyPressed(Key.Alt)}");

        if (Input.IsKeyPressed(Key.Alt))
        {
            // Alt+Click: 发送药水到聊天
            var segment = new TooltipSegment
            {
                Tooltip = PotionTooltip.FromModel(potion)
            };
            ProviderUtils.SendToChat(segment);
        }
        // 普通点击：不处理（holder 创建时 isUsable=false，不会打开使用弹窗）
    }

    #endregion

    #region IPlayerPanelProvider Implementation

    public string ProviderId => "potions";
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
        ProviderUtils.ClearChildren(container);

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

            // 订阅点击事件，支持 Alt+Click 发送药水到聊天
            holder.Connect(NClickableControl.SignalName.Released,
                Callable.From(() => OnPotionHolderReleased(holder, potion)));

            container.AddChild(holder);
            holder.AddPotion(nPotion);
            nPotion.Position = Vector2.Zero; // 关键：重置位置，否则会出现偏移
            ProviderUtils.SetPotionScale(holder, PotionScale);
            nPotion.Scale = Vector2.One * PotionScale;
            // 关键：设置 holder 的最小尺寸为缩小后的大小，否则 holder 仍占用原始大小
            holder.CustomMinimumSize = nPotion.Size * PotionScale;

            Log.Debug($"Added potion {potion.Id.Entry} to holder, MouseFilter={holder.MouseFilter}");
        }
    }

    public Action SubscribeEvents(Player player, Action onUpdate)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(onUpdate);
        Log.Debug($"SubscribeEvents for player with {player.Potions.Count()} potions");

        // 订阅药水变化事件
        // 事件类型是 Action<PotionModel>，需要适配
        void OnPotionChanged(PotionModel potion)
        {
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

    public void Cleanup(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ProviderUtils.ClearChildren(content);
    }

    #endregion
}

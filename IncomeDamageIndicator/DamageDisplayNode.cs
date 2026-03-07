using Godot;
using HarmonyLib;
using lemonSpire2.util;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace lemonSpire2.IncomeDamageIndicator;

/// <summary>
/// 伤害显示节点，在玩家头顶显示总伤害
/// </summary>
public partial class DamageDisplayNode : Node2D
{
    private Sprite2D? _intentIcon;
    private Label? _damageLabel;
    private Player? _player;
    private int _lastDamage = -1;

    public override void _Ready()
    {
        // 创建图标精灵（上下翻转）
        _intentIcon = new Sprite2D();
        _intentIcon.RotationDegrees = 180;
        _intentIcon.Scale = new Vector2(0.75f, 0.75f);
        _intentIcon.Position = Vector2.Zero;

        // 创建伤害数字标签（在图标右边）
        _damageLabel = new Label();
        _damageLabel.AddThemeColorOverride("font_color", new Color(1f, 0.2f, 0.2f));
        _damageLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f));
        _damageLabel.AddThemeConstantOverride("outline_size", 4);
        _damageLabel.AddThemeFontSizeOverride("font_size", 18);
        _damageLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _damageLabel.Position = new Vector2(25, -15);

        AddChild(_intentIcon);
        AddChild(_damageLabel);
    }

    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public override void _Process(double delta)
    {
        if (_player == null || !_player.Creature.IsAlive)
        {
            Visible = false;
            return;
        }

        if (NCombatRoom.Instance == null)
        {
            Visible = false;
            return;
        }

        UpdatePosition();

        int totalDamage = DamageCalculator.CalculateTotalIncomingDamage(_player);

        if (totalDamage != _lastDamage)
        {
            _lastDamage = totalDamage;
            UpdateDisplay(totalDamage);
        }
    }

    private void UpdatePosition()
    {
        if (_player == null)
            return;

        NCreature? creatureNode = NCombatRoom.Instance?.GetCreatureNode(_player.Creature);
        if (creatureNode != null)
        {
            // 放在玩家头顶上方
            GlobalPosition = creatureNode.VfxSpawnPosition + new Vector2(0, -120);
        }
    }

    private void UpdateDisplay(int totalDamage)
    {
        if (_intentIcon == null || _damageLabel == null)
            return;

        if (totalDamage <= 0)
        {
            Visible = false;
            return;
        }

        Visible = true;

        // 设置对应的意图图标
        _intentIcon.Texture = GetIntentIcon(totalDamage);

        // 设置伤害数字
        _damageLabel.Text = totalDamage.ToString();
    }

    /// <summary>
    /// 根据伤害值获取对应的意图图标
    /// </summary>
    private Texture2D GetIntentIcon(int damage)
    {
        string iconSuffix = damage switch
        {
            < 5 => "1",
            < 10 => "2",
            < 20 => "3",
            < 40 => "4",
            _ => "5"
        };

        string imagePath = ImageHelper.GetImagePath($"atlases/intent_atlas.sprites/attack/intent_attack_{iconSuffix}.tres");
        return PreloadManager.Cache.GetTexture2D(imagePath);
    }
}

/// <summary>
/// Harmony 补丁：在战斗房间准备好时创建伤害显示
/// </summary>
[HarmonyPatch(typeof(NCombatRoom))]
public static class NCombatRoomPatch
{
    private static readonly WeakNodeRegistry<DamageDisplayNode> _displays = new();

    [HarmonyPatch("_Ready")]
    [HarmonyPostfix]
    public static void Postfix_Ready(NCombatRoom __instance)
    {
        // _Ready 之后 CombatVfxContainer 已经初始化
        var display = new DamageDisplayNode();
        __instance.CombatVfxContainer.AddChild(display);
        _displays.Register(display);
        MainFile.Logger.Debug("DamageDisplayNode created and registered");
    }

    [HarmonyPatch("OnCombatSetUp")]
    [HarmonyPostfix]
    public static void Postfix_OnCombatSetUp(NCombatRoom __instance, CombatState state)
    {
        // 战斗设置完成后，设置玩家引用
        Player? me = LocalContext.GetMe(state);
        if (me == null)
            return;

        _displays.ForEachLive(display => display.SetPlayer(me));
    }

    [HarmonyPatch("_ExitTree")]
    [HarmonyPrefix]
    public static void Prefix_ExitTree()
    {
        // 清理所有显示节点
        _displays.ForEachLive(display => display.QueueFree());
        MainFile.Logger.Debug("DamageDisplayNode instances cleaned up");
    }
}
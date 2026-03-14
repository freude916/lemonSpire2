using Godot;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace lemonSpire2.util.Ui;

/// <summary>
///     药水包装容器，用于延迟初始化 NPotionHolder
///     NPotionHolder.AddPotion 需要 holder 在场景树中（_Ready 会初始化 _emptyIcon）
/// </summary>
public partial class PotionControlWrapper : Control
{
    private readonly NPotionHolder _holder;
    private readonly NPotion _potion;
    private readonly float _scale;

    public PotionControlWrapper(NPotionHolder holder, NPotion potion, float scale)
    {
        _holder = holder;
        _potion = potion;
        _scale = scale;
        CustomMinimumSize = new Vector2(32, 32);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Ready()
    {
        AddChild(_holder);
        _holder.AddPotion(_potion);
        _potion.Position = Vector2.Zero;
        ProviderUtils.SetPotionScale(_holder, _scale);
        _potion.Scale = Vector2.One * _scale;
    }
}

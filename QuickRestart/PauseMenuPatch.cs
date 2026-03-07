using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;

namespace lemonSpire2.QuickRestart;

[HarmonyPatch(typeof(NPauseMenu), "_Ready")]
public static class PauseMenuPatch
{
    /// <summary>
    /// 注入自定义按钮到暂停菜单
    /// </summary>
    public static NPauseMenuButton? InjectButton(
        Control buttonContainer,
        NPauseMenuButton templateButton,
        NPauseMenuButton insertBefore,
        string labelText,
        Action<NButton> onPressed,
        string buttonName = "CustomButton")
    {
        var newButton = (NPauseMenuButton)templateButton.Duplicate();
        newButton.Name = buttonName;

        // 复制材质避免共享hover状态
        var buttonImage = newButton.GetNode<TextureRect>("ButtonImage");
        if (buttonImage?.Material is ShaderMaterial material)
            buttonImage.Material = (ShaderMaterial)material.Duplicate();

        // 设置文本
        var label = newButton.GetNodeOrNull<MegaCrit.Sts2.addons.mega_text.MegaLabel>("Label");
        if (label != null)
            label.SetTextAutoSize(labelText);

        // 插入按钮
        int insertIndex = insertBefore.GetIndex();
        buttonContainer.AddChild(newButton);
        buttonContainer.MoveChild(newButton, insertIndex);

        // 连接信号
        newButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From(onPressed)
        );

        // 重建焦点邻居
        RebuildFocusNeighbors(buttonContainer);

        return newButton;
    }

    private static void RebuildFocusNeighbors(Control buttonContainer)
    {
        var buttons = new List<NPauseMenuButton>();
        foreach (Node child in buttonContainer.GetChildren())
        {
            if (child is NPauseMenuButton { Visible: true } btn)
                buttons.Add(btn);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            btn.FocusNeighborLeft = btn.GetPath();
            btn.FocusNeighborRight = btn.GetPath();
            btn.FocusNeighborTop = i > 0 ? buttons[i - 1].GetPath() : btn.GetPath();
            btn.FocusNeighborBottom = i < buttons.Count - 1 ? buttons[i + 1].GetPath() : btn.GetPath();
        }
    }
}
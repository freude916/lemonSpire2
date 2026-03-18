using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace lemonSpire2.ColorEx;

/// <summary>
///     远程鼠标颜色 Patch
///     修改远程玩家鼠标的颜色，使用 Shader 实现去色 + 着色效果
/// </summary>
[HarmonyPatchCategory("PlayerColor")]
[HarmonyPatch(typeof(NRemoteMouseCursor))]
public static class RemoteCursorColorPatch
{
    /// <summary>
    ///     描边亮度阈值：低于此值视为描边，涂成白色
    ///     原始材质描边为纯黑 RGB(0,0,0)，阈值 50 可覆盖抗锯齿边缘
    /// </summary>
    private const float OutlineLumThreshold = 50f / 255f;

    /// <summary>
    ///     去色着色 Shader 代码
    ///     1. 黑色描边涂成白色
    ///     2. 内部去色 + 着色
    /// </summary>
    private const string DesaturateShaderCode =
        """
        shader_type canvas_item;
        render_mode blend_mix;

        uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
        uniform float outline_lum_threshold = 0.2;

        void fragment() {
            vec4 tex_color = texture(TEXTURE, UV);

            if (tex_color.a < 0.1) {
                COLOR = vec4(0.0);
            } else {
                float lum = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));

                if (lum < outline_lum_threshold) {
                    COLOR = vec4(0.5, 0.5, 0.5, tex_color.a);
                } else {
                    vec3 result = lum * tint_color.rgb;
                    COLOR = vec4(result, tex_color.a);
                }
            }
        }
        """;

    /// <summary>
    ///     去色 + 着色 Shader（静态，只创建一次）
    /// </summary>
    private static Shader? _desaturateShader;

    private static readonly List<(WeakReference<NRemoteMouseCursor> Cursor, Action<ulong, Color> Handler)>
        Registrations = new();

    [HarmonyPostfix]
    [HarmonyPatch("_Ready")]
    public static void ReadyPostfix(NRemoteMouseCursor __instance)
    {
        var playerId = __instance.PlayerId;

        // 创建颜色变更回调
        Action<ulong, Color> handler = (changedPlayerId, color) =>
        {
            if (changedPlayerId == playerId) UpdateCursorColor(__instance, color);
        };

        // 使用弱引用存储，避免内存泄漏
        CleanupDeadReferences();
        Registrations.Add((new WeakReference<NRemoteMouseCursor>(__instance), handler));
        ColorManager.Instance.OnPlayerColorChanged += handler;

        // 设置初始颜色（默认是 DrawingMode.None）
        var customColor = ColorManager.Instance.GetCustomColor(playerId);
        if (customColor.HasValue) UpdateCursorColor(__instance, customColor.Value);
    }

    /// <summary>
    ///     当光标图像更新时，根据 DrawingMode 决定是否应用颜色
    ///     只对普通指针（DrawingMode.None）应用颜色，quill/pencil 保持原样
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch("UpdateImage")]
    public static void UpdateImagePostfix(NRemoteMouseCursor __instance, bool isDown, DrawingMode drawingMode)
    {
        var textureRect = __instance.GetNode<TextureRect>("TextureRect");
        if (textureRect == null) return;

        if (drawingMode == DrawingMode.None)
        {
            // 普通指针：应用玩家颜色
            var customColor = ColorManager.Instance.GetCustomColor(__instance.PlayerId);
            if (customColor.HasValue) UpdateCursorColor(__instance, customColor.Value);
        }
        else
        {
            // quill/pencil：清除材质，恢复原样
            textureRect.Material = null;
            textureRect.Modulate = Colors.White;
            textureRect.SelfModulate = Colors.White;
        }
    }

    private static void CleanupDeadReferences()
    {
        for (var i = Registrations.Count - 1; i >= 0; i--)
        {
            if (Registrations[i].Cursor.TryGetTarget(out var cursor) && GodotObject.IsInstanceValid(cursor)) continue;
            ColorManager.Instance.OnPlayerColorChanged -= Registrations[i].Handler;
            Registrations.RemoveAt(i);
        }
    }

    private static void UpdateCursorColor(NRemoteMouseCursor instance, Color playerColor)
    {
        var textureRect = instance.GetNode<TextureRect>("TextureRect");
        if (textureRect == null) return;

        // 确保 Shader 已创建
        _desaturateShader ??= CreateDesaturateShader();

        // 创建新的 ShaderMaterial（每个 TextureRect 独立，以便设置不同颜色）
        var material = new ShaderMaterial
        {
            Shader = _desaturateShader
        };

        // 设置着色参数
        material.SetShaderParameter("tint_color", playerColor);
        material.SetShaderParameter("outline_lum_threshold", OutlineLumThreshold);

        textureRect.Material = material;
    }

    private static Shader CreateDesaturateShader()
    {
        var shader = new Shader
        {
            Code = DesaturateShaderCode
        };
        // 强制初始化 shader，确保编译完成
        _ = shader.GetRid();
        return shader;
    }
}

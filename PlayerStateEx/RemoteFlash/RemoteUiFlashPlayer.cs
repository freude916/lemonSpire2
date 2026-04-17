using Godot;

namespace lemonSpire2.PlayerStateEx.RemoteFlash;

public static class RemoteUiFlashPlayer
{
    private static readonly StringName FlashTweenMeta = "_lemon_remote_flash_tween";
    private static readonly StringName FlashBaseModulateMeta = "_lemon_remote_flash_base_modulate";

    public static void FlashGreen(CanvasItem target, UiFlashOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!GodotObject.IsInstanceValid(target)) return;

        options ??= new UiFlashOptions();

        var original = GetOrCaptureBaseModulate(target);

        if (target.HasMeta(FlashTweenMeta) &&
            target.GetMeta(FlashTweenMeta).AsGodotObject() is Tween existingTween &&
            GodotObject.IsInstanceValid(existingTween))
        {
            existingTween.Kill();
            target.Modulate = original;
        }

        var flash = original.Lerp(new Color(0.2f, 1f, 0.2f, original.A), options.PeakStrength);

        var tween = target.CreateTween();
        for (var i = 0; i < Math.Max(1, options.Pulses); i++)
        {
            tween.TweenProperty(target, "modulate", flash, options.HalfDuration);
            tween.TweenProperty(target, "modulate", original, options.HalfDuration);
        }

        target.SetMeta(FlashTweenMeta, Variant.From(tween));
        tween.Finished += () =>
        {
            if (!GodotObject.IsInstanceValid(target)) return;
            if (!IsCurrentTween(target, tween)) return;
            target.Modulate = original;
            target.RemoveMeta(FlashTweenMeta);
            target.RemoveMeta(FlashBaseModulateMeta);
        };
    }

    private static Color GetOrCaptureBaseModulate(CanvasItem target)
    {
        if (target.HasMeta(FlashBaseModulateMeta))
            return target.GetMeta(FlashBaseModulateMeta).AsColor();

        var original = target.Modulate;
        target.SetMeta(FlashBaseModulateMeta, Variant.From(original));
        return original;
    }

    private static bool IsCurrentTween(CanvasItem target, Tween tween)
    {
        return target.HasMeta(FlashTweenMeta) &&
               target.GetMeta(FlashTweenMeta).AsGodotObject() is Tween currentTween &&
               ReferenceEquals(currentTween, tween);
    }
}

using Godot;

namespace lemonSpire2.Chat.Ui;

/// <summary>
///     Internal container handling input events and frame updates.
/// </summary>
internal sealed partial class ChatPanelContainer(ChatPanel owner) : PanelContainer
{
    private readonly WeakReference<ChatPanel> _ownerRef = new(owner);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        if (_ownerRef.TryGetTarget(out var owner))
            owner.Initialize();
    }

    public override void _ExitTree()
    {
        if (_ownerRef.TryGetTarget(out var owner))
            owner.Dispose();
    }

    public override void _Input(InputEvent @event)
    {
        if (_ownerRef.TryGetTarget(out var owner) && owner.HandleInput(@event))
            GetViewport()?.SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (_ownerRef.TryGetTarget(out var owner))
            owner.ProcessFrame(delta);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && _ownerRef.TryGetTarget(out var owner))
            owner.OnResized();
    }
}

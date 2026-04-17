using Godot;
using lemonSpire2.Chat.Input.Model;

namespace lemonSpire2.Chat.Ui.Completion;

internal sealed class ChatCompletionPopupController : IDisposable
{
    private const int MaxVisibleItems = 10;
    private const float ItemHeight = 28f;
    private const float PopupPadding = 8f;

    private readonly ItemList _list;
    private readonly PanelContainer _panel;
    private readonly ScrollContainer _scroll;
    private IReadOnlyList<ChatCompletionItem> _items = [];
    private ChatCompletionSession? _session;

    public ChatCompletionPopupController()
    {
        _panel = new PanelContainer
        {
            Name = "ChatCompletionPopup",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 100,
            TopLevel = true
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.08f, 0.08f, 0.95f),
            BorderColor = new Color(0.4f, 0.6f, 0.4f, 0.9f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        _panel.AddThemeStyleboxOverride("panel", style);

        _scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        _list = new ItemList
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SelectMode = ItemList.SelectModeEnum.Single,
            AutoHeight = true,
            SameColumnWidth = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _list.AddThemeFontSizeOverride("font_size", ChatConfig.FontSize);
        _scroll.AddChild(_list);
        _panel.AddChild(_scroll);
    }

    public bool IsOpen => _panel.Visible && _items.Count > 0 && _session is not null;

    public void Dispose()
    {
        _session = null;
        _items = [];
    }

    public void Attach(Control parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        parent.AddChild(_panel);
    }

    public void Hide()
    {
        _session = null;
        _items = [];
        _list.Clear();
        _panel.Visible = false;
    }

    public void Show(LineEdit inputField, ChatCompletionSession session, IReadOnlyList<ChatCompletionItem> items)
    {
        ArgumentNullException.ThrowIfNull(inputField);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            Hide();
            return;
        }

        _session = session;
        _items = items;
        _list.Clear();
        foreach (var item in items)
            _list.AddItem(item.DisplayText);

        _list.Select(0);
        var visibleItemCount = Mathf.Min(items.Count, MaxVisibleItems);
        var popupWidth = Mathf.Max(inputField.Size.X, 260f);
        var visibleHeight = visibleItemCount * ItemHeight;
        _panel.Size = new Vector2(popupWidth, visibleHeight + PopupPadding);
        _scroll.CustomMinimumSize = new Vector2(popupWidth - PopupPadding, visibleHeight);
        _list.CustomMinimumSize = new Vector2(popupWidth - PopupPadding, items.Count * ItemHeight);
        _scroll.ScrollVertical = 0;
        _panel.Position = inputField.GlobalPosition - new Vector2(0, _panel.Size.Y + 4f);
        _panel.Visible = true;
    }

    public bool MoveSelection(int delta)
    {
        if (!IsOpen)
            return false;

        var currentIndex = Math.Max(_list.GetSelectedItems().FirstOrDefault(-1), 0);
        var nextIndex = Mathf.Clamp(currentIndex + delta, 0, _items.Count - 1);
        _list.DeselectAll();
        _list.Select(nextIndex);
        _list.EnsureCurrentIsVisible();
        return true;
    }

    public bool TryConfirm(LineEdit inputField)
    {
        if (!IsOpen || _session is null)
            return false;

        var selectedIndex = _list.GetSelectedItems().FirstOrDefault(-1);
        if (selectedIndex < 0 || selectedIndex >= _items.Count)
            return false;

        var item = _items[selectedIndex];
        inputField.Text = inputField.Text.Remove(_session.ReplaceStart, _session.ReplaceLength)
            .Insert(_session.ReplaceStart, item.InsertText);
        inputField.CaretColumn = _session.ReplaceStart + item.InsertText.Length;
        Hide();
        return true;
    }
}

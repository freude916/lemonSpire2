namespace lemonSpire2.Chat.Input.Service.Mention;

public static class MentionTextCodec
{
    public static string Encode(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return displayName.Replace(" ", "_", StringComparison.InvariantCulture);
    }
}

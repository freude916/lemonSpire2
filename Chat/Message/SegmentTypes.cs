using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace lemonSpire2.Chat.Message;

public static class SegmentTypes
{
    private static readonly NetTypeCache<IMsgSegment> Cache = new(
        [.. ReflectionHelper.GetSubtypes<IMsgSegment>(), .. ReflectionHelper.GetSubtypesInMods<IMsgSegment>()]);

    public static int ToId(IMsgSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        return Cache.TypeToId(segment.GetType());
    }

    public static bool TryGetType(int id, out Type? type)
    {
        return Cache.TryGetTypeFromId(id, out type);
    }
}

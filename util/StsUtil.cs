using MegaCrit.Sts2.Core.Models;

namespace lemonSpire2.util;

public static class StsUtil
{
    public static T? ResolveModel<T>(string entry) where T : AbstractModel
    {
        return ModelDb.GetByIdOrNull<T>(new ModelId(ModelId.SlugifyCategory<T>(), entry));
    }
}


using Godot;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace lemonSpire2.util;

public static class StsUtil
{
    public static T? ResolveModel<T>(string entry) where T : AbstractModel
    {
        return ModelDb.GetByIdOrNull<T>(new ModelId(ModelId.SlugifyCategory<T>(), entry));
    }
}
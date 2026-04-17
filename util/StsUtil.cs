using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace lemonSpire2.util;

public static class StsUtil
{
    public static T? ResolveModel<T>(string entry) where T : AbstractModel
    {
        return ModelDb.GetByIdOrNull<T>(new ModelId(ModelId.SlugifyCategory<T>(), entry));
    }

    public static string GetPlayerNameFromNetId(ulong netId)
    {
        var runManager = RunManager.Instance;
        return PlatformUtil.GetPlayerName(runManager.NetService.Platform, netId);
    }

    public static Color GetRarityColor(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => RarityColor.Basic,
            CardRarity.Common => RarityColor.Common,
            CardRarity.Uncommon => RarityColor.Uncommon,
            CardRarity.Rare => RarityColor.Rare,
            CardRarity.Curse => RarityColor.Curse,
            CardRarity.Event => RarityColor.Event,
            CardRarity.Quest => RarityColor.Quest,
            CardRarity.Ancient => RarityColor.Ancient,
            CardRarity.Token => RarityColor.Token,
            CardRarity.Status => RarityColor.Status,
            _ => RarityColor.None
        };
    }

    public static Color GetRarityColor(PotionRarity rarity)
    {
        return rarity switch
        {
            PotionRarity.Common => RarityColor.Common,
            PotionRarity.Uncommon => RarityColor.Uncommon,
            PotionRarity.Rare => RarityColor.Rare,
            PotionRarity.Event => RarityColor.Event,
            PotionRarity.Token => RarityColor.Token,
            _ => RarityColor.None
        };
    }

    public static Color GetRarityColor(RelicRarity rarity)
    {
        return rarity switch
        {
            RelicRarity.Starter => RarityColor.Basic,
            RelicRarity.Common => RarityColor.Common,
            RelicRarity.Uncommon => RarityColor.Uncommon,
            RelicRarity.Rare => RarityColor.Rare,
            RelicRarity.Shop => RarityColor.Shop,
            RelicRarity.Event => RarityColor.Event,
            RelicRarity.Ancient => RarityColor.Ancient,
            _ => RarityColor.None
        };
    }

    public static class RarityColor
    {
        public static readonly Color None = new("FF0000FF");
        public static readonly Color Basic = new("9C9C9CFF");
        public static readonly Color Common = new("FFFFFFFF");
        public static readonly Color Uncommon = new("64FFFFFF");
        public static readonly Color Rare = new("FFDA36FF");
        public static readonly Color Curse = new("E669FFFF");
        public static readonly Color Event = new("13BE1AFF");
        public static readonly Color Quest = new("F46836FF");
        public static readonly Color Ancient = new("2B994EFF");
        public static readonly Color Token = new("497EA3FF");
        public static readonly Color Status = new("A34949FF");
        public static readonly Color Shop = new("298BCCFF");
    }
}

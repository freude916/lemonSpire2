namespace lemonSpire2;

[AttributeUsage(AttributeTargets.All)]
internal sealed class UsedImplicitlyAttribute : Attribute;

internal static class LemonSpireConfig
{
    internal const string SettingsDataKey = "settings";
    private static readonly SettingsModel FallbackSettings = CreateDefaultSettingsModel();

    public static bool EnableQoL
    {
        get => GetSettings().EnableQoL;
        set => Update(settings => settings.EnableQoL = value);
    }

    public static bool EnableChat
    {
        get => GetSettings().EnableChat;
        set => Update(settings => settings.EnableChat = value);
    }

    public static bool EnableSynergyIndicator
    {
        get => GetSettings().EnableSynergyIndicator;
        set => Update(settings => settings.EnableSynergyIndicator = value);
    }

    public static bool EnableStatsTracker
    {
        get => GetSettings().EnableStatsTracker;
        set => Update(settings => settings.EnableStatsTracker = value);
    }

    public static bool EnableSync
    {
        get => GetSettings().EnableSync;
        set => Update(settings => settings.EnableSync = value);
    }

    public static bool EnablePlayerColor
    {
        get => GetSettings().EnablePlayerColor;
        set => Update(settings => settings.EnablePlayerColor = value);
    }

    public static bool GroupHandCards
    {
        get => GetSettings().GroupHandCards;
        set => Update(settings => settings.GroupHandCards = value);
    }

    internal static SettingsModel CreateDefaultSettingsModel()
    {
        return new SettingsModel();
    }

    [UsedImplicitly]
    public static object CreateRitsuLibSettingsSchema()
    {
        return new
        {
            modId = MainFile.ModId,
            modDisplayName = Loc("LEMONSPIRE2.mod_title", "Lemon Spire 2"),
            pages = new object[]
            {
                new
                {
                    pageId = MainFile.ModId,
                    title = new
                    {
                        langMap = new
                        {
                            eng = "Settings",
                            zhs = "设置",
                            rus = "Настройки"
                        },
                        fallback = "Settings"
                    },
                    sections = new object[]
                    {
                        new
                        {
                            id = "feature_flags",
                            title = Loc("LEMONSPIRE2-FEATURE_FLAGS.title", "Feature Flags"),
                            entries = new object[]
                            {
                                new
                                {
                                    id = "enable_qol",
                                    type = "toggle",
                                    key = nameof(EnableQoL),
                                    label = Loc("LEMONSPIRE2-ENABLE_QO_L.title", "Enable QoL")
                                },
                                new
                                {
                                    id = "enable_chat",
                                    type = "toggle",
                                    key = nameof(EnableChat),
                                    label = Loc("LEMONSPIRE2-ENABLE_CHAT.title", "Enable Chat")
                                },
                                new
                                {
                                    id = "enable_synergy_indicator",
                                    type = "toggle",
                                    key = nameof(EnableSynergyIndicator),
                                    label = Loc("LEMONSPIRE2-ENABLE_SYNERGY_INDICATOR.title",
                                        "Enable Synergy Indicator")
                                },
                                new
                                {
                                    id = "enable_stats_tracker",
                                    type = "toggle",
                                    key = nameof(EnableStatsTracker),
                                    label = Loc("LEMONSPIRE2-ENABLE_STATS_TRACKER.title",
                                        "Enable Damage Stats Tracker")
                                },
                                new
                                {
                                    id = "enable_sync",
                                    type = "toggle",
                                    key = nameof(EnableSync),
                                    label = Loc("LEMONSPIRE2-ENABLE_SYNC.title", "Enable Sync")
                                },
                                new
                                {
                                    id = "enable_player_color",
                                    type = "toggle",
                                    key = nameof(EnablePlayerColor),
                                    label = Loc("LEMONSPIRE2-ENABLE_PLAYER_COLOR.title",
                                        "Enable Player Color")
                                }
                            }
                        },
                        new
                        {
                            id = "panel_flags",
                            title = Loc("LEMONSPIRE2-PANEL_FLAGS.title", "Panel Flags"),
                            entries = new object[]
                            {
                                new
                                {
                                    id = "group_hand_cards",
                                    type = "toggle",
                                    key = nameof(GroupHandCards),
                                    label = Loc("LEMONSPIRE2-GROUP_HAND_CARDS.title", "Group Hand Cards")
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    [UsedImplicitly]
    public static object? GetRitsuLibSettingValue(string key)
    {
        return key switch
        {
            nameof(EnableQoL) => EnableQoL,
            nameof(EnableChat) => EnableChat,
            nameof(EnableSynergyIndicator) => EnableSynergyIndicator,
            nameof(EnableStatsTracker) => EnableStatsTracker,
            nameof(EnableSync) => EnableSync,
            nameof(EnablePlayerColor) => EnablePlayerColor,
            nameof(GroupHandCards) => GroupHandCards,
            _ => null
        };
    }

    [UsedImplicitly]
    public static void SetRitsuLibSettingValue(string key, object value)
    {
        SetRitsuLibSettingBool(key, value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => Convert.ToBoolean(value)
        });
    }

    [UsedImplicitly]
    public static bool GetRitsuLibSettingBool(string key)
    {
        return key switch
        {
            nameof(EnableQoL) => EnableQoL,
            nameof(EnableChat) => EnableChat,
            nameof(EnableSynergyIndicator) => EnableSynergyIndicator,
            nameof(EnableStatsTracker) => EnableStatsTracker,
            nameof(EnableSync) => EnableSync,
            nameof(EnablePlayerColor) => EnablePlayerColor,
            nameof(GroupHandCards) => GroupHandCards,
            _ => false
        };
    }

    [UsedImplicitly]
    public static void SetRitsuLibSettingBool(string key, bool value)
    {
        switch (key)
        {
            case nameof(EnableQoL):
                EnableQoL = value;
                break;
            case nameof(EnableChat):
                EnableChat = value;
                break;
            case nameof(EnableSynergyIndicator):
                EnableSynergyIndicator = value;
                break;
            case nameof(EnableStatsTracker):
                EnableStatsTracker = value;
                break;
            case nameof(EnableSync):
                EnableSync = value;
                break;
            case nameof(EnablePlayerColor):
                EnablePlayerColor = value;
                break;
            case nameof(GroupHandCards):
                GroupHandCards = value;
                break;
        }
    }

    [UsedImplicitly]
    public static void SaveRitsuLibSettings()
    {
        if (!RitsuLibBridge.IsAvailable)
            return;

        RitsuLibBridge.SaveSettings();
    }

    internal static void TryInitializeRitsuLibBackends()
    {
        try
        {
            RitsuLibBridge.Initialize();
        }
        catch (Exception ex)
        {
            RitsuLibBridge.Disable();
            MainFile.Log.Info($"RitsuLib soft dependency unavailable: {ex.GetBaseException().Message}");
        }
    }

    private static SettingsModel GetSettings()
    {
        return RitsuLibBridge.IsAvailable ? RitsuLibBridge.GetSettings() : FallbackSettings;
    }

    private static void Update(Action<SettingsModel> update)
    {
        if (RitsuLibBridge.IsAvailable)
        {
            RitsuLibBridge.ModifySettings(update);
            return;
        }

        update(FallbackSettings);
    }

    private static object Loc(string key, string fallback)
    {
        return new
        {
            locString = new
            {
                table = "settings_ui",
                key,
                fallback
            }
        };
    }
}

internal sealed class SettingsModel
{
    public bool EnableQoL { get; set; } = true;
    public bool EnableChat { get; set; } = true;
    public bool EnableSynergyIndicator { get; set; } = true;
    public bool EnableStatsTracker { get; set; } = true;
    public bool EnableSync { get; set; } = true;
    public bool EnablePlayerColor { get; set; } = true;
    public bool GroupHandCards { get; set; } = true;
}

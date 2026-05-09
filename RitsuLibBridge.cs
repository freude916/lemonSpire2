using System.Reflection;

namespace lemonSpire2;

internal static class RitsuLibBridge
{
    private const string FrameworkTypeName = "STS2RitsuLib.RitsuLibFramework";
    private const string SaveScopeTypeName = "STS2RitsuLib.Utils.Persistence.SaveScope";
    private const string RegistrarTypeName = "STS2RitsuLib.Settings.ModSettingsRuntimeReflectionInteropMirror";
    private const string RitsuAssemblyName = "STS2-RitsuLib";

    private static readonly Lock Gate = new();
    private static RitsuLibApi? _api;

    public static bool IsAvailable => _api != null;

    public static void Initialize()
    {
        lock (Gate)
        {
            if (_api != null)
                return;

            var api = DiscoverApi();
            api.InitializeStore();
            api.RegisterProvider();
            _api = api;
        }
    }

    public static void Disable()
    {
        lock (Gate)
        {
            _api = null;
        }
    }

    public static SettingsModel GetSettings()
    {
        return _api!.GetSettings();
    }

    public static void ModifySettings(Action<SettingsModel> update)
    {
        _api!.ModifySettings(update);
    }

    public static void SaveSettings()
    {
        _api!.SaveSettings();
    }

    private static RitsuLibApi DiscoverApi()
    {
        var frameworkType = RequireType(FrameworkTypeName);
        var saveScopeType = RequireType(SaveScopeTypeName);
        var registrarType = RequireType(RegistrarTypeName);
        var storeType = RequireMethod(frameworkType, "GetDataStore", [typeof(string)]).ReturnType;

        var beginModDataRegistration = RequireMethod(frameworkType, "BeginModDataRegistration",
            [typeof(string), typeof(bool)]);
        var getDataStore = RequireMethod(frameworkType, "GetDataStore", [typeof(string)]);
        var registerSettings = RequireGenericMethod(storeType, "Register", 7).MakeGenericMethod(typeof(SettingsModel));
        var initializeGlobal = RequireMethod(storeType, "InitializeGlobal", Type.EmptyTypes);
        var getSettings = RequireGenericMethod(storeType, "Get", 1).MakeGenericMethod(typeof(SettingsModel));
        var modifySettings = RequireGenericMethod(storeType, "Modify", 2).MakeGenericMethod(typeof(SettingsModel));
        var saveSettings = RequireMethod(storeType, "Save", [typeof(string)]);
        var registerProvider = RequireMethod(registrarType, "RegisterProviderTypeAndTryRegister",
            [typeof(string), typeof(string)]);

        return new RitsuLibApi(
            beginModDataRegistration,
            getDataStore,
            registerSettings,
            initializeGlobal,
            getSettings,
            modifySettings,
            saveSettings,
            registerProvider,
            Enum.Parse(saveScopeType, "Global"));
    }

    private static Type RequireType(string fullName)
    {
        return FindType(fullName) ?? throw new InvalidOperationException($"{fullName} was not found.");
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => string.Equals(assembly.GetName().Name, RitsuAssemblyName,
                StringComparison.OrdinalIgnoreCase))
            .Select(assembly => assembly.GetType(fullName, false))
            .FirstOrDefault(type => type != null);
    }

    private static MethodInfo RequireMethod(Type declaringType, string name, Type[] parameterTypes)
    {
        return declaringType.GetMethod(
                   name,
                   BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                   null,
                   parameterTypes,
                   null)
               ?? throw new InvalidOperationException(
                   $"{declaringType.FullName}.{name}({string.Join(", ", parameterTypes.Select(type => type.Name))}) was not found.");
    }

    private static MethodInfo RequireGenericMethod(Type declaringType, string name, int parameterCount)
    {
        return declaringType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                   .FirstOrDefault(method =>
                       method.Name == name &&
                       method.IsGenericMethodDefinition &&
                       method.GetParameters().Length == parameterCount)
               ?? throw new InvalidOperationException(
                   $"{declaringType.FullName}.{name}<T>(...) with {parameterCount} parameters was not found.");
    }
}

internal sealed class RitsuLibApi(
    MethodInfo beginModDataRegistration,
    MethodInfo getDataStore,
    MethodInfo registerSettings,
    MethodInfo initializeGlobal,
    MethodInfo getSettings,
    MethodInfo modifySettings,
    MethodInfo saveSettings,
    MethodInfo registerProvider,
    object saveScopeGlobal)
{
    public void InitializeStore()
    {
        object? scope = null;
        try
        {
            scope = beginModDataRegistration.Invoke(null, [MainFile.ModId, true]);
            var store = GetStore();
            registerSettings.Invoke(store,
            [
                LemonSpireConfig.SettingsDataKey,
                "settings.json",
                saveScopeGlobal,
                (Func<SettingsModel>)LemonSpireConfig.CreateDefaultSettingsModel,
                true,
                null,
                null
            ]);
            initializeGlobal.Invoke(store, []);
        }
        finally
        {
            (scope as IDisposable)?.Dispose();
        }
    }

    public void RegisterProvider()
    {
        registerProvider.Invoke(null,
        [
            typeof(LemonSpireConfig).FullName!,
            typeof(LemonSpireConfig).Assembly.GetName().Name
        ]);
    }

    public SettingsModel GetSettings()
    {
        return (SettingsModel)getSettings.Invoke(GetStore(), [LemonSpireConfig.SettingsDataKey])!;
    }

    public void ModifySettings(Action<SettingsModel> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        modifySettings.Invoke(GetStore(), [LemonSpireConfig.SettingsDataKey, update]);
    }

    public void SaveSettings()
    {
        saveSettings.Invoke(GetStore(), [LemonSpireConfig.SettingsDataKey]);
    }

    private object? GetStore()
    {
        return getDataStore.Invoke(null, [MainFile.ModId]);
    }
}

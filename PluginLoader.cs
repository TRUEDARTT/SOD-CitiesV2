#region Usings
using BepInEx;
using HarmonyLib;
using SOD.Common.BepInEx;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using ObjectLoader;
#endregion

#region Base
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(AssetBundleLoader.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(ObjectLoader.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class CitiesV2 : PluginController<CitiesV2>
{
    public const string PluginGUID = "truedartt.citiesv2";
    public const string PluginName = "CitiesV2";
    public const string PluginVersion = "0.1.0";

    protected static JsonSerializerOptions serializerOptions = new JsonSerializerOptions { AllowTrailingCommas = true, WriteIndented = true, MaxDepth = 256};

    protected static string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    protected static List<FloorLoaderConfig> floorLoaderConfigs = new();

    /// <summary>
    /// Add your building manifest here. The string represents the extra folder it lies in.
    /// </summary>
    public static readonly Dictionary<string,BuildingManifestBase> buildingManifests = new();

    /// <summary>
    /// Add your rooms here. The first string represents the name of it.
    /// The second string is the directory the folder is located in (use Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).
    /// </summary>
    public static readonly Dictionary<string, string> rooms = new();

    #endregion





    public override void Load()
    {
        Log.LogInfo(Path.Combine(dir, "LoaderConfigs.json"));
        try
        {
            floorLoaderConfigs = JsonSerializer.Deserialize<List<FloorLoaderConfig>>(File.ReadAllText(Path.Combine(dir, "LoaderConfigs.json")), serializerOptions);
        }
        catch (Exception e)
        {
            CitiesV2.Log.LogError(e);
        }

        try
        {
            buildingManifests.Add("DartTower", JsonSerializer.Deserialize<BuildingManifestBase>(File.ReadAllText(Path.Combine(dir, "DartTower", "DartTower_manifest.json"))));
        }
        catch (Exception e)
        {
            CitiesV2.Log.LogError(e);
        }
        Loader.SetPath(dir);

        rooms.Add("Terrace", Path.Combine(dir, "RoomConfigs"));

        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.LogInfo($"Plugin {PluginName} is loaded!");
    }
}


#if DEBUG

[HarmonyPatch(typeof(Game),nameof(Game.Log))]
public class DEBUGGER
{
    public static bool Prefix(Il2CppSystem.Object __0, int __1)
    {
        UnityEngine.Debug.Log(__0);
        return false;
    }
}
#endif
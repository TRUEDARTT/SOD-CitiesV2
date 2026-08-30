using Il2CppInterop.Runtime;
using ObjectLoader;
using UnityEngine;
using static ObjectLoader.Loader;
using HarmonyLib;


public class RoomLoader : CitiesV2
{
    public static List<RoomConfiguration> roomConfigs = new();
    public static List<RoomTypePreset> presets = new();
    public static List<FiltersToAdd> filtersToAdd = new();
    public static void LoadAll()
    {
        foreach (var room in rooms)
        {
            LoadRoom(room.Key, room.Value);
        }
    }





    public static void LoadRoom(string name, string dir)
    {
        var cache = Toolbox.Instance.resourcesCache;

        // DROPPING THE OLD ONES IF THEY ARE THERE
        try 
        {
            var cacheRC = cache[Il2CppType.Of<RoomConfiguration>()];
            ScriptableObject.Destroy(cacheRC[name]);
            cacheRC.Remove(name);
            
            cacheRC = cache[Il2CppType.Of<RoomClassPreset>()];
            ScriptableObject.Destroy(cacheRC[name]);
            cacheRC.Remove(name);

            cacheRC = cache[Il2CppType.Of<RoomTypeFilter>()];
            ScriptableObject.Destroy(cacheRC[name]);
            cacheRC.Remove(name);

            cacheRC = cache[Il2CppType.Of<RoomTypePreset>()];
            ScriptableObject.Destroy(cacheRC[name]);
            cacheRC.Remove(name);
        }
        catch(Exception)
        {
            CitiesV2.Log.LogInfo("Nothing to drop (CUSTOM ROOM)");
        }

        var rc = LoadFromJson<RoomConfiguration>(Path.Combine(dir, name, name + "_config.json"));
        RoomClassPreset rClass = ScriptableObject.CreateInstance<RoomClassPreset>();
        rClass.presetName = name;
        rClass.name = name;
        Toolbox.Instance.resourcesCache[Il2CppType.Of<RoomClassPreset>()].Add(rClass.name, rClass);

        var preset = LoadFromJson<RoomTypePreset>(Path.Combine(dir, name, name + "_preset.json"));
        rc.roomType = preset;
        rc.roomClass = rClass;
        rc.openPlanRoom = preset;
        rc.debugRoom = rc;

        preset.name = name;
        rc.name = name;
        rc.steps = Helpers.GetScriptableObject("Hallway", typeof(RoomConfiguration)).Cast<RoomConfiguration>().steps;

        var filters = LoadFromJson<FiltersToAdd>(Path.Combine(dir, name, name + "_filters.json"));
        foreach (var filter in filters.Filters)
        {
            filter.roomClasses.Add(rClass);
#if DEBUG
            CitiesV2.Log.LogWarning($"Adding custom room {name} to {filter.presetName}");
#endif
        }

        foreach (var address in filters.Addresses)
        {
            address.roomConfig.Add(rc);
        }

        roomConfigs.Add(rc);
        presets.Add(preset);

        if (filters.MaterialGroups.Count == 0 && filters.Clusters.Count == 0 && filters.BasicFurniture.Count == 0) return;

        RoomTypeFilter dummy = ScriptableObject.CreateInstance(Il2CppType.Of<RoomTypeFilter>()).Cast<RoomTypeFilter>();
        dummy.presetName = name;
        dummy.name = name;
        dummy.roomClasses.Add(rClass);
        Toolbox.Instance.resourcesCache[Il2CppType.Of<RoomTypeFilter>()].Add(dummy.name, dummy);

        var hFurnSet = new Il2CppSystem.Collections.Generic.HashSet<FurniturePreset>();

        foreach (var furn in filters.BasicFurniture)
        {
            furn.allowedRoomFilters.Add(dummy);
            hFurnSet.Add(furn);
        }
        foreach (var mat in filters.MaterialGroups)
        {
            mat.allowedRoomFilters.Add(dummy);
        }
        foreach (var cl in filters.Clusters)
        {
            cl.allowedRoomFilters.Add(dummy);
            cl.enableDebug = true;
        }
        filtersToAdd.Add(filters);


        Toolbox.Instance.furnitureRoomTypeRef.Add(rClass, hFurnSet);
    }

}



[Serializable]
public class FiltersToAdd
{
    public List<RoomTypeFilter> Filters { get; set; } = new();
    public List<FurniturePreset> BasicFurniture { get; set; } = new();
    public List<FurnitureCluster> Clusters { get; set; } = new();
    public List<MaterialGroupPreset> MaterialGroups { get; set; } = new();
    public List<AddressPreset> Addresses { get; set; } = new();
}

#if DEBUG
[HarmonyPatch(typeof(GenerationController),nameof(GenerationController.GetValidFurniture))]
public class FurnishDebugAlpha
{

    public static bool Prefix(ref bool debug, ref bool ignoreLimitations)
    {
        debug = CitiesV2.debugFurniture;
        return true;
    }
}

[HarmonyPatch(typeof(GenerationController), nameof(GenerationController.GetBestFurnitureClusterLocation))]
public class FurnishDebugBeta
{
    public static bool Prefix(ref bool ignoreLimitations)
    {
        ignoreLimitations = CitiesV2.ignoreLimitations;
        return true;
    }
    public static void Postfix(ref NewRoom room, ref FurnitureCluster cluster, ref FurnitureClusterLocation __result)
    {
        if (__result  == null) CitiesV2.Log.LogError($"Furniture cluster {cluster.presetName} failed in {room.name}. Room Node count: {room.nodes.Count}");
    }
}

#endif

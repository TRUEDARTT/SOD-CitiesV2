using static ObjectLoader.Loader;
using Il2CppInterop.Runtime;
using UnityEngine;
using ObjectLoader;


public class RoomLoader : CitiesV2
{
    public static List<string> loaded = new List<string>();
    public static List<RoomConfiguration> roomConfigs = new();
    public static List<RoomTypePreset> presets = new();
    public static List<FiltersToAdd> filtersToAdd = new();
    public static void LoadAll()
    {
        foreach (var room in rooms)
        {
            if (loaded.Contains(room.Key)) return;
            loaded.Add(room.Key);
            LoadRoom(room.Key, room.Value);
        }
    }





    public static void LoadRoom(string name, string dir)
    {
        var cache = Toolbox.Instance.resourcesCache;

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
#if DEBUG
            cl.essentialFurniture = true;
#endif
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

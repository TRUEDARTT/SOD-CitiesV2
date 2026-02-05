using HarmonyLib;
using static ObjectLoader.Loader;



public class FloorRegistration : CitiesV2
{
    public static void Postfix()
    {
        CitiesV2.Log.LogMessage("Floor registration has begun.");
        var dict = CityData.Instance.floorData;
        foreach (var bd in BuildingLoader.buildings)
        {
            foreach (var lay in bd.preset.floorLayouts)
            {
                foreach (var bp in lay.blueprints) if (!dict.ContainsKey(bp.name)) dict.Add(bp.name, LoadObject<FloorSaveData>(bp.text));
                foreach (var bp in lay.controlRoomVariants) if(!dict.ContainsKey(bp.name)) dict.Add(bp.name, LoadObject<FloorSaveData>(bp.text));
            }
            foreach (var lay in bd.preset.basementLayouts)
            {
                foreach (var bp in lay.blueprints) if (!dict.ContainsKey(bp.name)) dict.Add(bp.name, LoadObject<FloorSaveData>(bp.text));
                foreach (var bp in lay.controlRoomVariants) if (!dict.ContainsKey(bp.name)) dict.Add(bp.name, LoadObject<FloorSaveData>(bp.text));
            }
        }
    }

}


[HarmonyPatch(typeof(BuildingCreator),nameof(BuildingCreator.Awake))]
public class BuildingRegistration : CitiesV2
{
    public static void Postfix(BuildingCreator __instance)
    {
        foreach (var building in BuildingLoader.buildings)
        {
            __instance.buildingPresets.Add(building.preset);
        }
    }
}





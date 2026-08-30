using UnityEngine;


#region Header
public class LoaderMethods : CitiesV2
{
    public static void ReplaceFloorData(List<string> replacementFloors, string targetBuilding, int targetFloorSetting, int floorsAmount)
    {
        BuildingPreset.InteriorFloorSetting? target = null;

        string pathBase = Path.Combine(dir, @"Floorplans\");

        foreach (var building in AssetLoader.Instance.allBuildingData)
        {
            if (building.name.ToLower() == targetBuilding.ToLower())
            {
                target = building.floorLayouts[targetFloorSetting];
                CitiesV2.Log.LogInfo($"Found target building: {targetBuilding} at setting: {targetFloorSetting}.");
                break;
            }
        }
        if (target == null) return;
        target.blueprints.Clear();
        target.floorsWithThisSetting = floorsAmount;
        foreach (string floor in replacementFloors)
        {
            target.blueprints.Add(new TextAsset(File.ReadAllText(Path.Combine(pathBase, floor + ".json"))));
            target.blueprints[replacementFloors.IndexOf(floor)].name = floor;
            CityData.Instance.floorData.Add(floor, JsonUtility.FromJson<FloorSaveData>(File.ReadAllText(Path.Combine(pathBase, floor + ".json"))));
        }

    }
}
#endregion

#region ConfigClasses
public class FloorLoaderConfig
{
    public List<string>? floorNames { get; set; }
    public string? targetBuilding { get; set; }
    public int targetFloorConfig { get; set; }
    public int floorsAmount { get; set; }

    public FloorLoaderConfig() { }
}



#endregion


#region Core

// [HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Start))] 
public class FloorLoader : CitiesV2
{
    public static Dictionary<string, FloorSaveData> floorSaveDatas = new();

    public static void Postfix()
    {
        foreach (var floor in floorLoaderConfigs)
        {
            string localDir = Path.Combine(dir, @"Floorplans\");
            bool exists = true;
            foreach (var name in floor.floorNames)
            {
                if (File.Exists(Path.Combine(localDir, name + ".json"))) continue;
                exists = false;
                break;
            }
            if (!exists) continue;

            LoaderMethods.ReplaceFloorData(floor.floorNames, floor.targetBuilding, floor.targetFloorConfig, floor.floorsAmount);
            CitiesV2.Log.LogInfo($"Replaced floor data for {floor.targetBuilding} at setting {floor.targetFloorConfig} with {floor.floorNames.Count} floors.");
        }
    }
}
#endregion
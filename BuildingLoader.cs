using HarmonyLib;
using ObjectLoader;
using SOD.Common.Extensions;
using System.Text.Json;
using UnityEngine;
using static ObjectLoader.Loader;
using static WindowFloorPatch;

[HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Start))]
public class BuildingLoader : CitiesV2
{
    /// <summary>
    /// For accessing stuff and debugging.
    /// </summary>
    public static List<BuildingManifest> buildings = new();

    public static List<UniverseLib.AssetBundle> bundles = new();

    public static List<string> loaded = new();
    // no idea if this will work
    public static void Postfix()
    {
#if DEBUG
        if (basicDebug)
        {
            Game.Instance.collectDebugData = true;
            Game.Instance.devMode = true;/*
            Game.Instance.debugDisplayRoads = true;
            Game.Instance.debugPathfinding = true;
            Game.Instance.enableCullingDebug = true;*/
            Game.Instance.printDebug = true;
        }
#endif

        //room n shit
        RoomLoader.LoadAll();


        loaded.Clear();
        foreach (var building in buildings)
        {
            AssetLoader.instance.allBuildingData.Remove(building.preset);
        }


        foreach (var baseMan in buildingManifests)
        {
            var man = LoadBuilding(baseMan.Value, dir, baseMan.Key);
            if (man != null) buildings.Add(man);
            else continue;

            Log.LogInfo("Buildings have been cached");

            foreach (var fix in baseMan.Value.Stairfixes)
            {
                StairwellFix.toRun.Add(fix);
            }

            Log.LogInfo("Stairfixes have been added.");
        }



        CityChaos.SetupChaos();
        WindowFixes.FixWindows();

        // FORCED EDITS
        ObjectLoader.Helpers.GetScriptableObject<AddressPreset>("SyncClinic").baseScore = 1;
        var rBar = Helpers.GetScriptableObject<RoomConfiguration>("RooftopBar");
        rBar.sceneClean = SessionData.SceneProfile.outdoors;
        rBar.sceneDirty = SessionData.SceneProfile.outdoors;

        FloorLoader.Postfix();

        FloorRegistration.Postfix();

        CitiesV2.Log.LogInfo("Everything went according to plan");
    }

    /// <summary>
    /// Used to load a building
    /// </summary>
    public static BuildingManifest LoadBuilding(BuildingManifestBase manifest, string baseDir, string extraDir)
    {
        TempChangePath(Path.Combine(baseDir, extraDir));
        BuildingManifest buildingManifest = new();
        buildingManifest.preset = LoadFromJson<BuildingPreset>(Path.Combine(baseDir, extraDir, manifest.BuildingPresetSO));

        buildingManifest.assetBundle = AssetBundleLoader.BundleLoader.LoadBundle(Path.Combine(baseDir, extraDir, manifest.BuildingAssetBundle));
        bundles.Add(buildingManifest.assetBundle);

        var objectList = buildingManifest.assetBundle.LoadAllAssets().ToList();
        var preset = buildingManifest.preset;

        buildingManifest.baseTexture = objectList.Find(x => x.name == manifest.BuildingTexture).Cast<Texture2D>();
        buildingManifest.windowMap = objectList.Find(x => x.name == manifest.BuildingWindowMap).Cast<Texture2D>();
        buildingManifest.addonMap = objectList.Find(x => x.name == manifest.BuildingAddonMap).Cast<Texture2D>();
        buildingManifest.litMap = objectList.Find(x => x.name == manifest.BuildingLitMap).Cast<Texture2D>();
        buildingManifest.unlitMap = objectList.Find(x => x.name == manifest.BuildingUnlitMap).Cast<Texture2D>();
        buildingManifest.prefab = objectList.Find(x => x.name == manifest.BuildingPrefab).Cast<GameObject>();

        buildingManifest.posOffset = manifest.PosOffset;

        preset.addonMap = buildingManifest.addonMap;
        preset.windowMap = buildingManifest.windowMap;
        preset.emissionMapLit = buildingManifest.litMap;
        preset.emissionMapUnlit = buildingManifest.unlitMap;
        preset.prefab = buildingManifest.prefab;
        preset.tex = buildingManifest.baseTexture;
        preset.captureMesh = preset.prefab.transform.Find("Base").GetComponent<MeshFilter>().sharedMesh;

        // ASSIGNING SHADERS
        var mat = preset.prefab.transform.Find("Base").GetComponent<MeshRenderer>().material;
        var newMat = Helpers.GetScriptableObject("OneFIfthAve", typeof(BuildingPreset)).Cast<BuildingPreset>().prefab.transform.Find("Base").GetComponent<MeshRenderer>().material;
        mat.shader = newMat.shader;
        // MAKING SURE IT DOES NOT GET DELETED
        preset.prefab.hideFlags = HideFlags.DontUnloadUnusedAsset;
        preset.hideFlags = HideFlags.DontUnloadUnusedAsset;
        preset.prefab.tag = "BuildingModel";

        preset.name = preset.presetName;
        
        preset.CalculateMeshHeight();


        foreach (var c in preset.prefab.transform)
        {
            Transform child = c.Cast<Transform>();
            if (child.name.Contains("Base"))
            {
                child.tag = "BuildingModelLights";
                child.gameObject.layer = 27;
            }
        }

        if (File.Exists(Path.Combine(baseDir, extraDir, manifest.CompiledWindowMap)))
        {
            preset.sortedWindows = Unpack(JsonSerializer.Deserialize<System.Collections.Generic.List<WindowFloorPatch>>(File.ReadAllText(Path.Combine(baseDir, extraDir, manifest.CompiledWindowMap))));
        }

        if (preset.sortedWindows == null && preset.windowMap != null)
        {
            preset.sortedWindows = new();
            preset.GenerateWindowData();
            File.WriteAllText(Path.Combine(baseDir, extraDir, manifest.CompiledWindowMap), JsonSerializer.Serialize(GeneratePatched(preset.sortedWindows), serializerOptions));
        }

        if (File.Exists(Path.Combine(baseDir, extraDir, manifest.CompiledAddonMap)))
        {
            preset.cableLinkPoints = JsonSerializer.Deserialize<System.Collections.Generic.List<BuildingPreset.CableLinkPoint>>(File.ReadAllText(Path.Combine(baseDir, extraDir, manifest.CompiledAddonMap)), serializerOptions).ToListIl2Cpp();

        }

        if ((preset.cableLinkPoints == null || preset.cableLinkPoints.Count == 0) && preset.addonMap != null)
        {
            preset.GenerateAddonData();
            File.WriteAllText(Path.Combine(baseDir, extraDir, manifest.CompiledAddonMap), JsonSerializer.Serialize(preset.cableLinkPoints.ToList(), serializerOptions));
        }
        RevertPath();

        AssetLoader.Instance.allBuildingData.Add(preset);
        return buildingManifest;
    }
}

#region Serializer Conversions
[Serializable]
public class WindowFloorPatch
{
    public System.Collections.Generic.List<WindowBlockPatch> Back { get; set; }
    public System.Collections.Generic.List<WindowBlockPatch> Front { get; set; }
    public System.Collections.Generic.List<WindowBlockPatch> Left { get; set; }
    public System.Collections.Generic.List<WindowBlockPatch> Right { get; set; }

    public static System.Collections.Generic.List<WindowFloorPatch> GeneratePatched(Il2CppSystem.Collections.Generic.List<BuildingPreset.WindowUVFloor> original)
    {
        System.Collections.Generic.List<WindowFloorPatch> patched = new();
        foreach (var floor in original)
        {
            WindowFloorPatch patch = new();
            patch.Left = new();
            patch.Right = new();
            patch.Front = new();
            patch.Back = new();

            foreach (var uv in floor.back)
            {
                uv.floor++;
                patch.Back.Add(GeneratePatched(uv));
            }
            foreach (var uv in floor.front)
            {
                uv.floor++;
                patch.Front.Add(GeneratePatched(uv));
            }
            foreach (var uv in floor.right)
            {
                uv.floor++;
                patch.Right.Add(GeneratePatched(uv));
            }
            foreach (var uv in floor.left)
            {
                uv.floor++;
                patch.Left.Add(GeneratePatched(uv));
            }
            patched.Add(patch);
        }
        return patched;
    }
    public static Il2CppSystem.Collections.Generic.List<BuildingPreset.WindowUVFloor> Unpack(System.Collections.Generic.List<WindowFloorPatch> patched)
    {
        Il2CppSystem.Collections.Generic.List<BuildingPreset.WindowUVFloor> original = new();

        foreach (var floor in patched)
        {
            BuildingPreset.WindowUVFloor og = new();
            og.left = new();
            og.right = new();
            og.front = new();
            og.back = new();

            foreach (var uv in floor.Left) og.left.Add(Unpack(uv));
            foreach (var uv in floor.Right) og.right.Add(Unpack(uv));
            foreach (var uv in floor.Front) og.front.Add(Unpack(uv));
            foreach (var uv in floor.Back) og.back.Add(Unpack(uv));

            original.Add(og);
        }

        return original;
    }

    [Serializable]
    public class WindowBlockPatch
    {
        // all of the og properties
        public float OriginPixelX { get; set; }
        public float OriginPixelY { get; set; }
        public float RectSizeX { get; set; }
        public float RectSizeY { get; set; }
        public float CentrePixelX { get; set; }
        public float CentrePixelY { get; set; }
        public float LocalMeshPositionLeftX { get; set; }
        public float LocalMeshPositionLeftY { get; set; }
        public float LocalMeshPositionLeftZ { get; set; }
        public float LocalMeshPositionRightX { get; set; }
        public float LocalMeshPositionRightY { get; set; }
        public float LocalMeshPositionRightZ { get; set; }
        public int Floor { get; set; }
        public float SideX { get; set; }
        public float SideY { get; set; }
        public int Horizonal { get; set; }
    }
    public static WindowBlockPatch GeneratePatched(BuildingPreset.WindowUVBlock src)
    {
        return new WindowBlockPatch
        {
            OriginPixelX = src.originPixel.x,
            OriginPixelY = src.originPixel.y,

            RectSizeX = src.rectSize.x,
            RectSizeY = src.rectSize.y,

            CentrePixelX = src.centrePixel.x,
            CentrePixelY = src.centrePixel.y,

            LocalMeshPositionLeftX = src.localMeshPositionLeft.x,
            LocalMeshPositionLeftY = src.localMeshPositionLeft.y,
            LocalMeshPositionLeftZ = src.localMeshPositionLeft.z,

            LocalMeshPositionRightX = src.localMeshPositionRight.x,
            LocalMeshPositionRightY = src.localMeshPositionRight.y,
            LocalMeshPositionRightZ = src.localMeshPositionRight.z,

            Floor = src.floor,

            SideX = src.side.x,
            SideY = src.side.y,

            Horizonal = src.horizonal
        };
    }

    public static BuildingPreset.WindowUVBlock Unpack(WindowBlockPatch src)
    {
        return new BuildingPreset.WindowUVBlock
        {
            originPixel = new Vector2(src.OriginPixelX, src.OriginPixelY),
            rectSize = new Vector2(src.RectSizeX, src.RectSizeY),
            centrePixel = new Vector2(src.CentrePixelX, src.CentrePixelY),

            localMeshPositionLeft = new Vector3(
                src.LocalMeshPositionLeftX,
                src.LocalMeshPositionLeftY,
                src.LocalMeshPositionLeftZ),

            localMeshPositionRight = new Vector3(
                src.LocalMeshPositionRightX,
                src.LocalMeshPositionRightY,
                src.LocalMeshPositionRightZ),

            floor = src.Floor,
            side = new Vector2(src.SideX, src.SideY),
            horizonal = src.Horizonal
        };
    }



}









#endregion

#region Building Manifest
[Serializable]
public class BuildingManifestBase
{
    public string BuildingPresetSO { get; set; }
    public string BuildingAssetBundle { get; set; }
    public string BuildingPrefab { get; set; }
    public string BuildingTexture { get; set; }
    public string BuildingUnlitMap { get; set; }
    public string BuildingLitMap { get; set; }
    public string? BuildingWindowMap { get; set; }
    public string? BuildingAddonMap { get; set; }
    public float PosOffset { get; set; }
    public string? CompiledWindowMap { get; set; }
    public string? CompiledAddonMap { get; set; }
    public System.Collections.Generic.List<Stairfix> Stairfixes { get; set; }
}

public class BuildingManifest
{
    public BuildingPreset? preset;
    public UniverseLib.AssetBundle? assetBundle;
    public GameObject? prefab;
    public Texture2D? baseTexture;
    public Texture2D? unlitMap;
    public Texture2D? litMap;
    public Texture2D? windowMap;
    public Texture2D? addonMap;
    public float posOffset;
    public System.Collections.Generic.List<Stairfix>? Stairfixes;
    public BuildingManifestBase? baseManifest;
}
#endregion
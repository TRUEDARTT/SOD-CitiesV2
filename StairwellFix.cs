using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Player),nameof(Player.PrepForStart))]
public class StairwellFix : CitiesV2
{
    public static List<Stairfix> toRun = new();

    [Serializable]
    public class NodePair
    {
        public Vector2Int nodeOne { get; set; }
        public Vector2Int nodeTwo { get; set; }

    }

    public static void Postfix()
    {
        Log.LogMessage("Fixing Stair connections...");
        // i also have to fix the posoffset not working
        foreach (var man in BuildingLoader.buildings)
        {
            foreach (var building in CityBuildings.Instance.buildingDirectory)
            {
                if (building.preset != man.preset) continue;
                foreach (var m in building.gameObject.transform)
                {
                    Transform mesh = m.Cast<Transform>();
                    if (mesh.name != man.preset.prefab.name + "(Clone)") continue;
                    Log.LogInfo($"OG position {mesh.position}");
                    mesh.position = new(mesh.position.x, mesh.position.y + man.posOffset, mesh.position.z);
                    Log.LogInfo($"NEW position {mesh.position}");
                }
            }
        }

        foreach (var fix in toRun)
        {
            Log.LogInfo($"Fixing: {fix.floorPresetIndex}(floorIndex) at {fix.preset}.");
            FixStairwell(fix);
        }
        //toRun.Clear();
    }



    public static Vector2Int CalculatePos(Vector2Int pos, int rotation, int grid = 21) //21 is the default
    {
        rotation = rotation % 4;


        switch (rotation)
        {
            case 1: return new Vector2Int(grid - 1 - pos.y, pos.x);
            case 2: return new Vector2Int(grid - 1 - pos.x, grid - 1 - pos.y);
            case 3: return new Vector2Int(pos.y, grid - 1 - pos.x);
            default: return pos;
        }
    }

    public static void FixStairwell(Stairfix stairfix)
    {

        List<NewBuilding> buildings = new();
        List<NewFloor> floors = new();

        BuildingPreset realPreset = Toolbox.Instance.resourcesCache[Il2CppInterop.Runtime.Il2CppType.Of<BuildingPreset>()][stairfix.preset].Cast<BuildingPreset>();
        List<string> targets = new();

        if (stairfix.isBasement)
        {
            foreach (var bp in realPreset.basementLayouts[stairfix.floorPresetIndex].blueprints)
            {
                targets.Add(bp.name);
            }
            foreach (var bp in realPreset.basementLayouts[stairfix.floorPresetIndex].controlRoomVariants)
            {
                targets.Add(bp.name);
            }
        }
        else
        {
            foreach (var bp in realPreset.floorLayouts[stairfix.floorPresetIndex].blueprints)
            {
                targets.Add(bp.name);
            }
            foreach (var bp in realPreset.floorLayouts[stairfix.floorPresetIndex].controlRoomVariants)
            {
                targets.Add(bp.name);
            }
        }

        foreach (var building in CityBuildings.Instance.buildingDirectory)
        {
            if (building.preset.name == stairfix.preset) buildings.Add(building);
        }
        foreach (var building in buildings)
        {
            int rot = building.rotations;
            List<NodePair> nodes = new List<NodePair>();
            foreach (NodePair pair in stairfix.stairConnectors)
            {
                nodes.Add(new NodePair() { nodeOne = CalculatePos(pair.nodeOne, rot, 21), nodeTwo = CalculatePos(pair.nodeTwo, rot, 21) });
            }


            foreach (var floor in building.floors.Values)
            {

                if (targets.Contains(floor.floorName))
                {
                    Dictionary<NodePair, (NewNode? one, NewNode? two)> nodeMap = new();
                    foreach (var node in nodes)
                    {
                        nodeMap.Add(node, new());
                    }

                    foreach (var address in floor.addresses)
                    {
                        foreach (var node in address.nodes)
                        {
                            foreach (var pair in nodes)
                            {
                                if (pair.nodeOne == node.floorCoord)
                                {
                                    nodeMap[pair] = (node, nodeMap[pair].two);
                                    continue;
                                }
                                if (pair.nodeTwo == node.floorCoord)
                                {
                                    nodeMap[pair] = (nodeMap[pair].one, node);
                                    continue;
                                }
                            }
                        }
                    }
                    foreach (var pair in nodeMap.Values)
                    {
                        pair.one.AddAccessToOtherNode(pair.two, true, true, NewNode.NodeAccess.AccessType.adjacent, true);
                    }
                }
            }
        }
    }
}

[Serializable]
public class Stairfix
{
    public List<StairwellFix.NodePair> stairConnectors { get; set; }
    public string preset { get; set; }
    public int floorPresetIndex { get; set; }
    public bool isBasement { get; set; }
}



// SO THAT CABLES WONT EXTEND
[HarmonyPatch(typeof(Elevator), nameof(Elevator.UpdateCables))]
public class ElevatorFix
{
    public static bool Prefix(Elevator __instance)
    {
        if (__instance.top != null && __instance.cable1 != null)
        {
            float y = __instance.top.position.y + PathFinder.Instance.tileSize.z - __instance.cable1.position.y;
            __instance.cable1.localScale = new Vector3(1f, y, 1f);
            __instance.cable2.localScale = new Vector3(1f, y, 1f);
        }
        return false;
    }
}
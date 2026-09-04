using SOD.Common.Extensions;
using static ObjectLoader.Helpers;



public class CityChaos : CitiesV2
{
    public class FurnitureChaosPreset
    {
        public List<string> FurnitureClusters { get; set; }
        public List<string> FurnitureClasses { get; set; }
        public List<string> DoubleFrontageRoomConfigs { get; set; }
        

        public FurnitureChaosPreset()
        {
            FurnitureClasses = new List<string>();
            FurnitureClusters = new List<string>();
            DoubleFrontageRoomConfigs = new List<string>();
        }
        
    }

    public static void SetupChaos()
    {
        var preset = presets[selectedPreset];

        foreach (var tempCluster in preset.FurnitureClusters)
        {
            var cluster = GetScriptableObject(tempCluster, typeof(FurnitureCluster)).Cast<FurnitureCluster>();
            if (cluster == null) continue;
            cluster.limitToFloor = false;
            cluster.limitToFloorRange = false;
            cluster.allowedInOpenPlan = FurnitureCluster.AllowedOpenPlan.yes;
        }
        foreach (var tempClass in preset.FurnitureClasses)
        {
            var furnClass = GetScriptableObject(tempClass, typeof(FurnitureClass)).Cast<FurnitureClass>();
            if (furnClass == null) continue;
            furnClass.limitToFloor = false;
            furnClass.limitToFloorRange= false;
            furnClass.requiresCeiling = false;
        }
        foreach (var tempConfig in preset.DoubleFrontageRoomConfigs)
        {
            var config = GetScriptableObject(tempConfig, typeof(RoomConfiguration)).Cast<RoomConfiguration>();
            if (config == null) continue;
            foreach (var frontage in config.wallFrontage)
            {
                frontage.onlyIfBorderingOutside = true;
                foreach (var insideFrontage in frontage.insideFrontage)
                {
                    if (!frontage.outsideFrontage.Contains(insideFrontage)) frontage.outsideFrontage.Add(insideFrontage);
                }
                foreach (var outsideFrontage in frontage.outsideFrontage)
                {
                    if (!frontage.insideFrontage.Contains(outsideFrontage)) frontage.insideFrontage.Add(outsideFrontage);
                }
            }

        }

        //TEMPORARRY
        GetScriptableObject("ParkBench", typeof(FurnitureCluster)).Cast<FurnitureCluster>().allowInResidential = true;
    }



    public static string selectedPreset = "Base";

    #region PRESETS
    public static Dictionary<string, FurnitureChaosPreset> presets = new()
    {
        {
            "Base",
            new() 
            {
                FurnitureClusters = 
                {
                    "SupermarketCounterLeft",
                    "SupermarketCounterRight",
                    "SupermarketFrigeUnitsX3",
                    "SupermarketFrigeUnitsX3 2",
                    "DisplayCounter",
                    "DrinksCoolersX2",
                    "DrinksCoolersX2 1",
                    "VendingMachines",
                    "ParkBench"
                },
                FurnitureClasses =
                {
                    "2x1SupermarketCounterRight",
                    "2x1SupermarketCounterLeft",
                    "2x1SupermarketEndShelf",
                    "2x1ElGenSyncMachine",
                    "2x1DisplayCounter",
                    "1x1VendingMachine",
                    "1x1ATM",
                    "1x1SupermarketDrinksCooler",
                    "1x1SupermarketEmptyShelf",
                    "1x1SupermarketFreezerBackward",
                    "1x1SupermarketFreezerCornerBackward",
                    "1x1SupermarketFreezerCornerForward",
                    "1x1SupermarketFreezerForward",
                    "1x1SupermarketFridge",
                    "1x1SupermarketFruitStandBackward",
                    "1x1SupermarketFruitStandForward",
                    "1x1SupermarketMagazineStand",
                    "1x1SupermarketScales",
                    "1x1SupermarketShelvingBackward",
                    "1x1SupermarketShelvingForward",
                    "1x1SupermarketShelvingSign",
                    "1x1SupermarketStand",
                    "2x1ParkBenchSingle",
                    "2x1ParkBenchStreetCentre"

                },
                DoubleFrontageRoomConfigs =
                {
                    "SyncClinic",
                    "Supermarket",
                    "Chemist",
                    "PawnShop",
                    "Launderette",
                    "HardwareStore",
                    "CorporateLobby",
                    "AsianDiningRoom",
                    "BarDiningRoom",
                    "FastFoodDiningRoom"
                }
            }
        }
    };

    #endregion
}

public class WindowFixes : CitiesV2
{
    // TARGET: RoomConfigurations that do not have a window.
    public static string[] windowFixes =
    {
        "GamblingDen",
        "WeaponsDealer",
        "BlackmarketSyncClinic",
        "BasementGenericDen",
        "BasementGenericBathroom",
        "LoanShark",
        "BlackmarketTrader",
        "IndustrialOffice"
    };

    public static void FixWindows()
    {
        foreach(var configName in windowFixes)
        {
            var config = GetScriptableObject(configName, typeof(RoomConfiguration)).Cast<RoomConfiguration>();
            if (config == null) 
            {
                Log.LogError($"Room configuration '{configName}' not found.");
                continue; 
            }

            Il2CppSystem.Collections.Generic.List<WallFrontageClass> outsideFrontage = new List<WallFrontageClass>()
            {
                GetScriptableObject<WallFrontageClass>("LargeRectangeWindowBlinds")
            }.ToListIl2Cpp();

            bool archExists = false;
            bool rectExists = false;

            foreach (var wallFrontage in config.wallFrontage)
            {
                if(wallFrontage.wallPreset.id == "15")
                {
                    archExists = true;
                    wallFrontage.insideFrontage = outsideFrontage;
                    wallFrontage.outsideFrontage = outsideFrontage;
                }
                if(wallFrontage.wallPreset.id == "16")
                {
                    rectExists = true;
                    wallFrontage.insideFrontage = outsideFrontage;
                    wallFrontage.outsideFrontage = outsideFrontage;
                }
            }



            if(!archExists)
            {
                var frontage = new RoomConfiguration.WallFrontage()
                {
                    name = "WindowFix",
                    wallPreset = GetScriptableObject<DoorPairPreset>("15"),
                    outsideFrontage = outsideFrontage,
                    insideFrontage = outsideFrontage
                };
                config.wallFrontage.Add(frontage);
            }

            if (!rectExists)
            {
                var frontage2 = new RoomConfiguration.WallFrontage()
                {
                    name = "WindowFix",
                    wallPreset = GetScriptableObject<DoorPairPreset>("16"),
                    outsideFrontage = outsideFrontage,
                    insideFrontage = outsideFrontage
                };
                config.wallFrontage.Add(frontage2);
            }
        }
    }
}

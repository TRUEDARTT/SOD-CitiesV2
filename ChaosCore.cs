using System;
using System.Collections.Generic;
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

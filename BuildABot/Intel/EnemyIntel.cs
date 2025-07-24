using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;
using Units;

namespace Intel
{
    public class EnemyIntel
    {
        public Race EnemyRace { get; private set; } = Race.Random;

        public HashSet<UnitType> SeenUnits { get; private set; } = new();
        public Dictionary<UnitType, int> SeenUnitCounts { get; private set; } = new();

        public HashSet<UnitType> SeenBuildings { get; private set; } = new();
        public Dictionary<UnitType, int> SeenBuildingCounts { get; private set; } = new();

        public Dictionary<UnitType, List<UnitType>> BuildingToPossibleUnits { get; private set; } = new();

        public int EstimatedEnemyBases { get; set; } = 1;

        public void UpdateEnemyRace(Race race)
        {
            if (EnemyRace == Race.Random && race != Race.Random)
            {
                EnemyRace = race;
            }
        }

        public void AddSeenUnit(UnitType type)
        {
            SeenUnits.Add(type);
            if (!SeenUnitCounts.ContainsKey(type))
                SeenUnitCounts[type] = 0;
            SeenUnitCounts[type]++;
        }

        public void AddSeenBuilding(UnitType type)
        {
            SeenBuildings.Add(type);
            if (!SeenBuildingCounts.ContainsKey(type))
                SeenBuildingCounts[type] = 0;
            SeenBuildingCounts[type]++;
        }

        public void InitializeBuildingUnitMapping()
        {
            // Example mappings (expand per race later)
            BuildingToPossibleUnits[UnitType.Barracks] = new List<UnitType> { UnitType.Marine, UnitType.Marauder, UnitType.Ghost, UnitType.Reaper };
            BuildingToPossibleUnits[UnitType.Factory] = new List<UnitType> { UnitType.Hellion, UnitType.SiegeTank, UnitType.Thor, UnitType.Cyclone };
            BuildingToPossibleUnits[UnitType.Starport] = new List<UnitType> { UnitType.Medivac, UnitType.VikingFighter, UnitType.Liberator, UnitType.Banshee, UnitType.Battlecruiser };

            BuildingToPossibleUnits[UnitType.Gateway] = new List<UnitType> { UnitType.Zealot, UnitType.Stalker, UnitType.Sentry, UnitType.Adept };
            BuildingToPossibleUnits[UnitType.RoboFacility] = new List<UnitType> { UnitType.Observer, UnitType.WarpPrism, UnitType.Immortal, UnitType.Colossus, UnitType.Disruptor };
            BuildingToPossibleUnits[UnitType.Stargate] = new List<UnitType> { UnitType.Phoenix, UnitType.VoidRay, UnitType.Oracle, UnitType.Carrier, UnitType.Tempest };

            BuildingToPossibleUnits[UnitType.Hatchery] = new List<UnitType> { UnitType.Zergling, UnitType.Drone };
            BuildingToPossibleUnits[UnitType.Larva] = new List<UnitType> { UnitType.Zergling, UnitType.Baneling, UnitType.Roach, UnitType.Hydralisk, UnitType.Mutalisk, UnitType.Infestor, UnitType.Ultralisk };
            BuildingToPossibleUnits[UnitType.SpawningPool] = new List<UnitType> { UnitType.Zergling, UnitType.Baneling };
            BuildingToPossibleUnits[UnitType.HydraliskDen] = new List<UnitType> { UnitType.Hydralisk, UnitType.Lurker };
        }

        public List<UnitType> GetLikelyUnitsFromBuilding(UnitType buildingType)
        {
            return BuildingToPossibleUnits.ContainsKey(buildingType)
                ? BuildingToPossibleUnits[buildingType]
                : new List<UnitType>();
        }
    }
}

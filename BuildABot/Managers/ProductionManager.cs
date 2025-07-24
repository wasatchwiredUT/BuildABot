using System.Collections.Generic;
using SC2APIProtocol;
using Units;
using Action = SC2APIProtocol.Action;

namespace Managers
{
    /// <summary>
    /// Responsible for training units and constructing structures.  It uses
    /// information about existing units to find appropriate producers and
    /// queues the required training commands.  This class encapsulates
    /// repetitive boilerplate such as determining which building trains a
    /// particular unit and issuing the correct ability.
    /// </summary>
    public class ProductionManager
    {
        private readonly List<Unit> _playerUnits;

        public ProductionManager(List<Unit> playerUnits)
        {
            _playerUnits = playerUnits;
        }

        /// <summary>
        /// Creates the specified number of units by issuing training commands on
        /// available production buildings.  If no producers are idle, the
        /// requested count may not be fully satisfied.
        /// </summary>
        public List<Action> CreateUnit(UnitType unitType, int count)
        {
            List<Action> actions = new List<Action>();
            int produced = 0;
            while (produced < count)
            {
                Unit producer = FindProducerFor(unitType);
                if (producer == null)
                {
                    // No more available producers; abort remaining requests.
                    break;
                }
                AbilityId trainAbility = GetTrainingAbility(unitType);
                if (trainAbility == 0)
                {
                    // Unknown training ability; abort.
                    break;
                }
                Action action = BuildTrainAction(producer, trainAbility);
                actions.Add(action);
                produced++;
            }
            return actions;
        }

        /// <summary>
        /// Finds an idle production structure that can train the specified unit.
        /// Returns null if no suitable building is available.
        /// </summary>
        private Unit FindProducerFor(UnitType unitType)
        {
            UnitType requiredProducer = UnitType.CommandCenter;
            UnitType alternateProducer = UnitType.CommandCenter;
            // Determine the producer type based on unit
            switch (unitType)
            {
                case UnitType.SCV:
                    requiredProducer = UnitType.CommandCenter;
                    break;
                case UnitType.Marine:
                    requiredProducer = UnitType.Barracks;
                    alternateProducer = UnitType.BarracksReactor;
                    break;
                case UnitType.Marauder:
                    requiredProducer = UnitType.BarracksTechLab;
                    break;
                case UnitType.Reaper:
                    requiredProducer = UnitType.Barracks;
                    alternateProducer = UnitType.BarracksReactor;
                    break;
                case UnitType.SiegeTank:
                    requiredProducer = UnitType.FactoryTechLab;
                    break;
                case UnitType.Medivac:
                    requiredProducer = UnitType.Starport;
                    alternateProducer = UnitType.StarportReactor;
                    break;
                case UnitType.Raven:
                    requiredProducer = UnitType.StarportTechLab;
                    break;

                // Additional Terran units
                case UnitType.Ghost:
                    requiredProducer = UnitType.BarracksTechLab;
                    break;
                case UnitType.Hellion:
                case UnitType.Hellbat:
                    requiredProducer = UnitType.Factory;
                    alternateProducer = UnitType.FactoryReactor;
                    break;
                case UnitType.Cyclone:
                    requiredProducer = UnitType.Factory;
                    alternateProducer = UnitType.FactoryReactor;
                    break;
                case UnitType.Thor:
                    requiredProducer = UnitType.FactoryTechLab;
                    break;
                case UnitType.Banshee:
                    requiredProducer = UnitType.StarportTechLab;
                    break;
                case UnitType.Battlecruiser:
                    requiredProducer = UnitType.StarportTechLab;
                    break;
                case UnitType.VikingFighter:
                    requiredProducer = UnitType.Starport;
                    alternateProducer = UnitType.StarportReactor;
                    break;
                case UnitType.Liberator:
                    requiredProducer = UnitType.Starport;
                    alternateProducer = UnitType.StarportReactor;
                    break;
                case UnitType.WidowMine:
                    requiredProducer = UnitType.Factory;
                    alternateProducer = UnitType.FactoryReactor;
                    break;
            }

            foreach (Unit unit in _playerUnits)
            {
                // Skip units that are not our required producer
                UnitType type = (UnitType)unit.UnitType;
                if (type != requiredProducer && type != alternateProducer)
                    continue;
                // Only select idle producers
                if (unit.Orders != null && unit.Orders.Count > 0)
                    continue;
                return unit;
            }
            return null;
        }

        /// <summary>
        /// Maps a unit type to the corresponding training ability.  Returns 0
        /// when the ability is not defined in the enum.
        /// </summary>
        private AbilityId GetTrainingAbility(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.SCV:
                    return AbilityId.Train_SCV;
                case UnitType.Marine:
                    return AbilityId.Train_Marine;
                case UnitType.Marauder:
                    return AbilityId.Train_Marauder;
                case UnitType.Reaper:
                    return AbilityId.Train_Reaper;
                case UnitType.SiegeTank:
                    return AbilityId.Train_SiegeTank;
                case UnitType.Medivac:
                    return AbilityId.Train_Medivac;
                case UnitType.Raven:
                    return AbilityId.Train_Raven;

                // Additional units
                case UnitType.Ghost:
                    return AbilityId.Train_Ghost;
                case UnitType.Hellion:
                    return AbilityId.Train_Hellion;
                case UnitType.Hellbat:
                    return AbilityId.Train_Hellbat;
                case UnitType.Cyclone:
                    return AbilityId.Train_Cyclone;
                case UnitType.Thor:
                    return AbilityId.Train_Thor;
                case UnitType.Banshee:
                    return AbilityId.Train_Banshee;
                case UnitType.Battlecruiser:
                    return AbilityId.Train_Battlecruiser;
                case UnitType.VikingFighter:
                    return AbilityId.Train_VikingFighter;
                case UnitType.Liberator:
                    return AbilityId.Train_Liberator;
                case UnitType.WidowMine:
                    return AbilityId.Train_WidowMine;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Constructs a training action for the specified producer and ability.
        /// </summary>
        private Action BuildTrainAction(Unit producer, AbilityId ability)
        {
            Action action = new Action();
            action.ActionRaw = new ActionRaw();
            action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
            action.ActionRaw.UnitCommand.UnitTags.Add(producer.Tag);
            action.ActionRaw.UnitCommand.AbilityId = (int)ability;
            return action;
        }
    }
}

using SC2APIProtocol;
using Units;
using Ancestors;
using Action = SC2APIProtocol.Action;
using Managers;
using Tasks;

namespace Controllers
{
    /// <summary>
    /// High‑level bot controller implementing the SC2API_CSharp.Bot interface.  This
    /// class exposes natural language style methods for training units and
    /// commanding specific unit types.  It maintains internal lists of your
    /// units and delegates production to a ProductionManager.  The OnFrame
    /// method is responsible for keeping these lists up to date each frame.
    /// </summary>
    public class BotController : Bot
    {


        private readonly ProductionManager _production = new ProductionManager();

        private readonly List<Unit> _ourUnits = new List<Unit>();

        private ScvScoutTask _scvScout;

        // Empty constructor
        public BotController()
        {
           
        }

        /// <summary>
        /// Called once at the beginning of the game.  We do not need to do
        /// special setup here but the signature is required by the interface.
        /// </summary>
        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentID)
        {
            _scvScout = new ScvScoutTask();
            _scvScout.OnStart(gameInfo);
        }

        /// <summary>
        /// Called every game frame.  This method updates our internal unit lists
        /// and constructs a list of actions to perform.  Users of the framework
        /// should build custom logic here using the natural language helpers.
        /// </summary>
        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            List<Action> actions = new List<Action>();
            // Update our unit list
            _ourUnits.Clear();
            if (observation.Observation != null && observation.Observation.RawData != null)
            {
                IList<Unit> units = observation.Observation.RawData.Units;
                for (int i = 0; i < units.Count; i++)
                {
                    Unit unit = units[i];
                    if (unit.Alliance == Alliance.Self)
                    {
                        _ourUnits.Add(unit);
                    }
                }
            }
            // Update production manager with latest units
            _production.SetPlayerUnits(_ourUnits);

          

            if (_wallManager != null)
            {
                actions.AddRange(_wallManager.MaintainWall(observation, _ourUnits));
            }

            if (_scvScout != null)
            {
                actions.AddRange(_scvScout.OnFrame(observation, _ourUnits));
            }

            // Insert high level bot logic here.  For example:
            // If we have less than 20 SCVs, produce more SCVs.
            int scvCount = 0;
            for (int i = 0; i < _ourUnits.Count; i++)
            {
                if ((UnitType)_ourUnits[i].UnitType == UnitType.SCV)
                {
                    scvCount++;
                }
            }
            if (scvCount < 20)
            {
                List<Action> scvActions = _production.CreateUnit(UnitType.SCV, 1);
                actions.AddRange(scvActions);
            }
            return actions;
        }

        /// <summary>
        /// Called when the game ends.  This implementation does nothing but is
        /// required by the interface.
        /// </summary>
        public void OnEnd(ResponseObservation observation, Result result)
        {
            // You can add end game logic here if needed
        }

        /// <summary>
        /// Natural language wrapper around ProductionManager.CreateUnit.
        /// </summary>
        public List<Action> CreateUnit(UnitType unitType, int count)
        {
            if (_production == null)
            {
                return new List<Action>();
            }
            return _production.CreateUnit(unitType, count);
        }

        /// <summary>
        /// Returns a list of our Siege Tank wrappers.  Both sieged and unsieged
        /// tanks are included.
        /// </summary>
        public List<SiegeTank> GetMySiegeTanks()
        {
            List<SiegeTank> list = new List<SiegeTank>();
            for (int i = 0; i < _ourUnits.Count; i++)
            {
                Unit unit = _ourUnits[i];
                UnitType type = (UnitType)unit.UnitType;
                if (type == UnitType.SiegeTank || type == UnitType.SiegeTankSieged)
                {
                    list.Add(new SiegeTank(unit));
                }
            }
            return list;
        }

        /// <summary>
        /// Returns a list of our Ravens.
        /// </summary>
        public List<Raven> GetMyRavens()
        {
            List<Raven> list = new List<Raven>();
            for (int i = 0; i < _ourUnits.Count; i++)
            {
                Unit unit = _ourUnits[i];
                UnitType type = (UnitType)unit.UnitType;
                if (type == UnitType.Raven)
                {
                    list.Add(new Raven(unit));
                }
            }
            return list;
        }

        /// <summary>
        /// Returns a list of our Marines.
        /// </summary>
        public List<Marine> GetMyMarines()
        {
            List<Marine> list = new List<Marine>();
            for (int i = 0; i < _ourUnits.Count; i++)
            {
                Unit unit = _ourUnits[i];
                if ((UnitType)unit.UnitType == UnitType.Marine)
                {
                    list.Add(new Marine(unit));
                }
            }
            return list;
        }
    }
}
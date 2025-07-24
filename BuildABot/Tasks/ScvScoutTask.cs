using System.Collections.Generic;
using SC2APIProtocol;
using Units;
using Action = SC2APIProtocol.Action;

namespace Tasks
{
    /// <summary>
    /// Simple task to send an SCV to scout the enemy base at game start.
    /// As the scout moves, its positions are recorded for basic pathing data.
    /// </summary>
    public class ScvScoutTask
    {
        private ResponseGameInfo _gameInfo;
        private ulong _scvTag;
        private bool _initialized;
        private readonly Queue<Point2D> _waypoints = new();
        private readonly List<Point2D> _path = new();

        public IReadOnlyList<Point2D> Path => _path;

        public void OnStart(ResponseGameInfo gameInfo)
        {
            _gameInfo = gameInfo;
        }

        private static float Distance(Point pos, Point2D p)
        {
            float dx = pos.X - p.X;
            float dy = pos.Y - p.Y;
            return (float)System.Math.Sqrt(dx * dx + dy * dy);
        }

        private Point2D FindOurStart(ResponseObservation obs)
        {
            foreach (var unit in obs.Observation.RawData.Units)
            {
                if (unit.Alliance == Alliance.Self)
                {
                    UnitType t = (UnitType)unit.UnitType;
                    if (t == UnitType.CommandCenter || t == UnitType.OrbitalCommand || t == UnitType.PlanetaryFortress)
                        return new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                }
            }
            return _gameInfo.StartRaw.StartLocations.Count > 0 ? _gameInfo.StartRaw.StartLocations[0] : null;
        }

        private Point2D GetEnemyStart(Point2D ourStart)
        {
            Point2D enemy = null;
            float max = -1f;
            foreach (var loc in _gameInfo.StartRaw.StartLocations)
            {
                float dx = loc.X - ourStart.X;
                float dy = loc.Y - ourStart.Y;
                float dist = dx * dx + dy * dy;
                if (dist > max)
                {
                    max = dist;
                    enemy = loc;
                }
            }
            return enemy;
        }

        private void SetupWaypoints(Point2D enemy)
        {
            _waypoints.Enqueue(enemy);
            float r = 5f;
            _waypoints.Enqueue(new Point2D { X = enemy.X + r, Y = enemy.Y });
            _waypoints.Enqueue(new Point2D { X = enemy.X, Y = enemy.Y + r });
            _waypoints.Enqueue(new Point2D { X = enemy.X - r, Y = enemy.Y });
            _waypoints.Enqueue(new Point2D { X = enemy.X, Y = enemy.Y - r });
        }

        public List<Action> OnFrame(ResponseObservation obs, List<Unit> ourUnits)
        {
            List<Action> actions = new();
            if (_gameInfo == null)
                return actions;

            if (!_initialized)
            {
                _initialized = true;
                Point2D our = FindOurStart(obs);
                Point2D enemy = GetEnemyStart(our);
                SetupWaypoints(enemy);
            }

            if (_scvTag == 0)
            {
                foreach (var u in ourUnits)
                {
                    if ((UnitType)u.UnitType == UnitType.SCV)
                    {
                        _scvTag = u.Tag;
                        break;
                    }
                }
            }

            Unit scvUnit = null;
            foreach (var u in ourUnits)
            {
                if (u.Tag == _scvTag)
                {
                    scvUnit = u;
                    break;
                }
            }
            if (scvUnit == null)
                return actions;

            _path.Add(new Point2D { X = scvUnit.Pos.X, Y = scvUnit.Pos.Y });
            SCV scv = new SCV(scvUnit);
            if (_waypoints.Count > 0)
            {
                Point2D target = _waypoints.Peek();
                if (Distance(scvUnit.Pos, target) < 1.0f)
                {
                    _waypoints.Dequeue();
                    if (_waypoints.Count == 0)
                        return actions;
                    target = _waypoints.Peek();
                }
                actions.Add(scv.Move(target));
            }
            return actions;
        }
    }
}

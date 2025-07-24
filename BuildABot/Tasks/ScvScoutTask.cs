using System.Collections.Generic;
using SC2APIProtocol;
using Units;
using Utilities;

using Action = SC2APIProtocol.Action;

namespace Tasks
{
    /// <summary>
    /// SCV scout that endlessly circles the enemy base, avoiding blocked waypoints.
    /// Retreats to our base when taking damage.
    /// </summary>
    public class ScvScoutTask
    {
        private ResponseGameInfo _gameInfo;
        private ulong _scvTag;
        private bool _initialized;
        private readonly Queue<Point2D> _waypoints = new();
        private readonly List<Point2D> _path = new();
        private bool _retreating;

        public IReadOnlyList<Point2D> Path => _path;

        public void OnStart(ResponseGameInfo gameInfo)
        {
            _gameInfo = gameInfo;
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

        private void SetupWaypoints(Point2D center, ResponseObservation obs, float radius = 10f, int segments = 48)
        {
            _waypoints.Clear();
            var enemyUnits = obs.Observation.RawData.Units;

            for (int i = 0; i < segments; i++)
            {
                float angle = 2 * System.MathF.PI * i / segments;
                float x = center.X + radius * System.MathF.Cos(angle);
                float y = center.Y + radius * System.MathF.Sin(angle);
                var point = new Point2D { X = x, Y = y };

                bool blocked = false;
                foreach (var enemy in enemyUnits)
                {
                    if (enemy.Alliance != Alliance.Enemy) continue;
                    if ((UnitType)enemy.UnitType == UnitType.SCV) continue;

                    if (PathingHelper.Distance(enemy.Pos, point) < 1.5f)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                    _waypoints.Enqueue(point);
            }
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
                SetupWaypoints(enemy, obs);
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

            if (!_retreating && scvUnit.Health < scvUnit.HealthMax)
            {
                _retreating = true;
            }

            if (_retreating)
            {
                Point2D retreatPoint = FindOurStart(obs);
                actions.Add(scv.Move(retreatPoint));

                if (PathingHelper.Distance(scvUnit.Pos, retreatPoint) < 2.0f && scvUnit.Health >= scvUnit.HealthMax * 0.95f)
                {
                    _retreating = false;
                }
                return actions;
            }

            if (_waypoints.Count > 0)
            {
                Point2D target = _waypoints.Peek();
                if (PathingHelper.Distance(scvUnit.Pos, target) < 1.0f || scvUnit.Orders.Count == 0)
                {
                    _waypoints.Dequeue();
                    _waypoints.Enqueue(target);
                    target = _waypoints.Peek();
                }

                actions.Add(scv.Move(target));
            }

            return actions;
        }
    }
}

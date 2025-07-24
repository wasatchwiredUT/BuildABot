using SC2APIProtocol;
using Units;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = SC2APIProtocol.Action;

namespace Managers
{
    /// <summary>
    /// Maintains a wall of structures at the main ramp. This class computes
    /// build locations using placement and pathing data from StartRaw and
    /// issues build commands when structures are missing.
    /// </summary>
    public class WallManager
    {
        private readonly ResponseGameInfo _gameInfo;
        private readonly List<Point2D> _wallPositions = new();
        private bool _initialized;

        public WallManager(ResponseGameInfo gameInfo)
        {
            _gameInfo = gameInfo;
        }

        /// <summary>
        /// Compute wall positions based on the map data and first observation.
        /// </summary>
        public void Initialize(ResponseObservation observation)
        {
            if (_initialized)
                return;
            _initialized = true;

            Point2D startLoc = FindStartLocation(observation);
            if (startLoc == null)
                return;

            var placement = _gameInfo.StartRaw.PlacementGrid;
            var pathing = _gameInfo.StartRaw.PathingGrid;
            int placeWidth = placement.Size.X;
            int placeHeight = placement.Size.Y;
            int pathWidth = pathing.Size.X;
            int pathHeight = pathing.Size.Y;
            byte[] placeData = placement.Data.ToByteArray();
            byte[] pathData = pathing.Data.ToByteArray();

            List<Point2D> rampCells = new();
            int sx = (int)Math.Round(startLoc.X);
            int sy = (int)Math.Round(startLoc.Y);
            int radius = 12;
            int maxX = Math.Min(placeWidth, pathWidth);
            int maxY = Math.Min(placeHeight, pathHeight);
            for (int x = Math.Max(0, sx - radius); x < Math.Min(maxX, sx + radius); x++)
            {
                for (int y = Math.Max(0, sy - radius); y < Math.Min(maxY, sy + radius); y++)
                {
                    int placeIndex = x + y * placeWidth;
                    int pathIndex = x + y * pathWidth;
                    if (pathIndex >= 0 && pathIndex < pathData.Length && placeIndex >= 0 && placeIndex < placeData.Length &&
                        pathData[pathIndex] == 255 && placeData[placeIndex] == 0)
                    {
                        rampCells.Add(new Point2D { X = x + 0.5f, Y = y + 0.5f });
                    }
                }
            }
            if (rampCells.Count == 0)
                return;

            float avgX = rampCells.Average(p => p.X);
            float avgY = rampCells.Average(p => p.Y);
            Point2D rampCenter = new Point2D { X = avgX, Y = avgY };

            float dirX = startLoc.X - rampCenter.X;
            float dirY = startLoc.Y - rampCenter.Y;
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len == 0) len = 1f;
            dirX /= len;
            dirY /= len;
            float perpX = -dirY;
            float perpY = dirX;

            for (int i = -1; i <= 1; i++)
            {
                float px = rampCenter.X + dirX * 1.5f + perpX * i * 2f;
                float py = rampCenter.Y + dirY * 1.5f + perpY * i * 2f;
                Point2D pos = FindNearestBuildable(px, py, placeData, placeWidth, placeHeight);
                if (pos != null)
                    _wallPositions.Add(pos);
            }
        }

        private Point2D FindStartLocation(ResponseObservation observation)
        {
            foreach (var unit in observation.Observation.RawData.Units)
            {
                UnitType type = (UnitType)unit.UnitType;
                if (unit.Alliance == Alliance.Self && (type == UnitType.CommandCenter || type == UnitType.OrbitalCommand || type == UnitType.PlanetaryFortress))
                    return new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
            }
            return _gameInfo.StartRaw.StartLocations.Count > 0 ? _gameInfo.StartRaw.StartLocations[0] : null;
        }

        private Point2D FindNearestBuildable(float x, float y, byte[] placement, int width, int height)
        {
            int ix = (int)Math.Round(x);
            int iy = (int)Math.Round(y);
            int max = 5;
            for (int r = 0; r <= max; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int px = ix + dx;
                        int py = iy + dy;
                        if (px < 0 || py < 0 || px >= width || py >= height)
                            continue;
                        int idx = px + py * width;
                        if (placement[idx] == 255)
                            return new Point2D { X = px + 0.5f, Y = py + 0.5f };
                    }
                }
            }
            return null;
        }

        private float Distance(Point pos, Point2D target)
        {
            float dx = pos.X - target.X;
            float dy = pos.Y - target.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private SCV GetIdleScv(List<Unit> units)
        {
            foreach (var unit in units)
            {
                if ((UnitType)unit.UnitType == UnitType.SCV && (unit.Orders == null || unit.Orders.Count == 0))
                    return new SCV(unit);
            }
            return null;
        }

        /// <summary>
        /// Checks the wall each frame and issues build commands when structures are missing.
        /// </summary>
        public List<Action> MaintainWall(ResponseObservation observation, List<Unit> ourUnits)
        {
            if (!_initialized)
                Initialize(observation);
            List<Action> actions = new();
            if (_wallPositions.Count == 0)
                return actions;

            int minerals = (int)observation.Observation.PlayerCommon.Minerals;
            foreach (var pos in _wallPositions)
            {
                bool hasStructure = false;
                foreach (var unit in ourUnits)
                {
                    UnitType t = (UnitType)unit.UnitType;
                    if ((t == UnitType.SupplyDepot || t == UnitType.SupplyDepotLowered || t == UnitType.Bunker || t == UnitType.Barracks) && Distance(unit.Pos, pos) < 1.0)
                    {
                        hasStructure = true;
                        break;
                    }
                }
                if (!hasStructure && minerals >= 100)
                {
                    SCV scv = GetIdleScv(ourUnits);
                    if (scv != null)
                    {
                        actions.Add(scv.BuildStructure(UnitType.SupplyDepot, pos));
                        minerals -= 100;
                    }
                }
            }
            return actions;
        }
    }
}

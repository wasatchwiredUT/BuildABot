using SC2APIProtocol;
using Units;
using Utilities;
using Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = SC2APIProtocol.Action;
using System.Diagnostics;

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
        private bool _scoutUsed;
        private IPathFinder _pathFinder;
        private MapDataService _mapDataService;
        private BuildingService _buildingService;
        private ChokePointService _chokePointService;
        private Point2D _startLoc;
        private byte[] _placementData;
        private int _placeWidth;
        private int _placeHeight;
        public WallManager(ResponseGameInfo gameInfo)
        {
            _gameInfo = gameInfo;
            _pathFinder = new AStarPathfinder(_gameInfo);
            _mapDataService = new MapDataService(_gameInfo);
            _buildingService = new BuildingService(_mapDataService);
            _chokePointService = new ChokePointService(_pathFinder, _mapDataService, _buildingService);
        }

        /// <summary>
        /// Compute wall positions based on the map data and first observation.
        /// </summary>
        public void Initialize(ResponseObservation observation, int scanRadius = 12)
        {
            if (_initialized) return;

            _startLoc = FindStartLocation(observation);
            if (_startLoc == null) return;

            var placement = _gameInfo.StartRaw.PlacementGrid;
            var pathing = _gameInfo.StartRaw.PathingGrid;
            _placeWidth = placement.Size.X;
            _placeHeight = placement.Size.Y;
            int pathWidth = pathing.Size.X, pathHeight = pathing.Size.Y;
            _placementData = placement.Data.ToByteArray();

            // Pick a distant target (another start location or map centre)
            Point2D target = null;
            float maxDist = -1f;
            foreach (var loc in _gameInfo.StartRaw.StartLocations)
            {
                if (loc.Equals(_startLoc)) continue;
                float d = (loc.X - _startLoc.X) * (loc.X - _startLoc.X) +
                          (loc.Y - _startLoc.Y) * (loc.Y - _startLoc.Y);
                if (d > maxDist) { maxDist = d; target = loc; }
            }
            if (target == null)
                target = new Point2D { X = pathWidth / 2f, Y = pathHeight / 2f };

            // Use choke point service to locate the ramp between our base and the target
            List<Point2D> rampCells = new();
            try
            {
                // allow a larger search distance to improve ramp detection on
                // wide maps
                var choke = _chokePointService.FindDefensiveChokePoint(_startLoc, target, 0, 60f);
                if (choke != null)
                {
                    rampCells = _chokePointService.GetEntireChokePoint(choke);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WallManager] Exception during choke point detection: {ex.Message}\n{ex.StackTrace}");
            }

            // fallback to simple scan if the chokepoint method found nothing
            if (rampCells.Count == 0)
            {
                try
                {
                    var pathPoints = _pathFinder.FindPath(_startLoc, target);
                    bool collecting = false;
                    foreach (var pt in pathPoints)
                    {
                        int x = (int)Math.Floor(pt.X);
                        int y = (int)Math.Floor(pt.Y);
                        int placeIndex = x + y * _placeWidth;
                        bool walkable = _mapDataService.PathWalkable(x, y);
                        bool unbuildable = _placementData[placeIndex] == 0;

                        if (walkable && unbuildable)
                        {
                            collecting = true;
                            rampCells.Add(new Point2D { X = x + 0.5f, Y = y + 0.5f });
                        }
                        else if (collecting)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine($"[WallManager] Exception during fallback ramp detection: {ex2.Message}\n{ex2.StackTrace}");
                }
            }

            if (rampCells.Count == 0) return;

            // compute the ramp’s centre and choose three buildable spots (unchanged)
            float avgX = rampCells.Average(p => p.X);
            float avgY = rampCells.Average(p => p.Y);
            Point2D rampCenter = new() { X = avgX, Y = avgY };

            float dirX = _startLoc.X - rampCenter.X;
            float dirY = _startLoc.Y - rampCenter.Y;
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len == 0) len = 1f;
            dirX /= len; dirY /= len;
            float perpX = -dirY, perpY = dirX;

            for (int i = -1; i <= 1; i++)
            {
                float px = rampCenter.X + dirX * 1.5f + perpX * i * 2f;
                float py = rampCenter.Y + dirY * 1.5f + perpY * i * 2f;
                Point2D pos = FindNearestBuildable(px, py, _placementData, _placeWidth, _placeHeight);
                if (pos != null) _wallPositions.Add(pos);
            }
            _initialized = true;
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
                        if (placement[idx] == 1)
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

        private SCV FindBuilder(List<Unit> units)
        {
            foreach (var unit in units)
            {
                if ((UnitType)unit.UnitType == UnitType.SCV)
                    return new SCV(unit);
            }
            return null;
        }

        /// <summary>
        /// Update the wall positions using a recorded scout path. This allows
        /// ramp detection even when the initial search fails.
        /// </summary>
        public void UpdateFromScoutPath(IReadOnlyList<Point2D> path)
        {
            if (_initialized || path == null || path.Count < 2 || _scoutUsed)
                return;

            var choke = _chokePointService.FindChokePointFromPath(path.ToList(), 60f);
            if (choke == null) return;
            var rampCells = _chokePointService.GetEntireChokePoint(choke);
            if (rampCells.Count == 0) return;

            float avgX = rampCells.Average(p => p.X);
            float avgY = rampCells.Average(p => p.Y);
            Point2D rampCenter = new() { X = avgX, Y = avgY };

            float dirX = _startLoc.X - rampCenter.X;
            float dirY = _startLoc.Y - rampCenter.Y;
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len == 0) len = 1f;
            dirX /= len; dirY /= len;
            float perpX = -dirY, perpY = dirX;

            _wallPositions.Clear();
            for (int i = -1; i <= 1; i++)
            {
                float px = rampCenter.X + dirX * 1.5f + perpX * i * 2f;
                float py = rampCenter.Y + dirY * 1.5f + perpY * i * 2f;
                Point2D pos = FindNearestBuildable(px, py, _placementData, _placeWidth, _placeHeight);
                if (pos != null) _wallPositions.Add(pos);
            }

            if (_wallPositions.Count > 0)
            {
                _initialized = true;
                _scoutUsed = true;
            }
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
                    SCV scv = FindBuilder(ourUnits); // Changed from GetIdleScv
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

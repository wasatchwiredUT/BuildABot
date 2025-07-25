using SC2APIProtocol;
using System.Collections.Generic;
using System.Linq;

namespace Pathing
{
    public class ChokePointService
    {
        private readonly Utilities.AStarPathfinder _pathFinder;
        private readonly MapDataService _mapDataService;
        private readonly BuildingService _buildingService;

        public ChokePointService(Utilities.AStarPathfinder pathFinder, MapDataService mapDataService, BuildingService buildingService)
        {
            _pathFinder = pathFinder;
            _mapDataService = mapDataService;
            _buildingService = buildingService;
        }

        public Point2D FindDefensiveChokePoint(Point2D start, Point2D end, int frame, float maxDistance = 30f)
        {
            var path = _pathFinder.FindPath(start, end);
            var choke = FindHighGroundChokePoint(path, maxDistance);
            if (choke != null) return choke;
            return FindLowGroundChokePoint(path, maxDistance);
        }

        public Point2D FindHighGroundChokePoint(List<Point2D> path, float maxDistance = 30f)
        {
            if (path.Count == 0) return null;
            int startHeight = _mapDataService.GetMapHeight(path[0]);
            Point2D prev = path[0];
            foreach (var p in path)
            {
                if (startHeight > _mapDataService.GetMapHeight(p))
                {
                    if (DistSq(path[0], p) > maxDistance * maxDistance) return null;
                    return prev;
                }
                prev = p;
            }
            return null;
        }

        public Point2D FindLowGroundChokePoint(List<Point2D> path, float maxDistance = 30f)
        {
            if (path.Count == 0) return null;
            int startHeight = _mapDataService.GetMapHeight(path[0]);
            Point2D prev = path[0];
            foreach (var p in path)
            {
                if (startHeight < _mapDataService.GetMapHeight(p))
                {
                    if (DistSq(path[0], p) > maxDistance * maxDistance) return null;
                    return prev;
                }
                prev = p;
            }
            return null;
        }

        private static float DistSq(Point2D a, Point2D b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        public List<Point2D> GetEntireChokePoint(Point2D chokePoint)
        {
            var points = new List<Point2D> { chokePoint };
            int height = _mapDataService.GetMapHeight(chokePoint);
            for (int x = -5; x < 10; x++)
            {
                for (int y = -5; y < 10; y++)
                {
                    int px = x + (int)chokePoint.X;
                    int py = y + (int)chokePoint.Y;
                    if (_mapDataService.MapHeightValue(px, py) == height && _mapDataService.PathWalkable(px, py))
                    {
                        if (TouchingLowerPoint(px, py, height))
                        {
                            points.Add(new Point2D { X = px, Y = py });
                        }
                    }
                }
            }
            return points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        }

        public List<Point2D> GetEntireBottomOfRamp(Point2D chokePoint)
        {
            var points = new List<Point2D>();
            int height = _mapDataService.GetMapHeight(chokePoint);
            for (int x = -5; x < 10; x++)
            {
                for (int y = -5; y < 10; y++)
                {
                    int px = x + (int)chokePoint.X;
                    int py = y + (int)chokePoint.Y;
                    if (_mapDataService.MapHeightValue(px, py) == height && _mapDataService.PathWalkable(px, py))
                    {
                        if (TouchingHigherPoint(px, py, height))
                        {
                            points.Add(new Point2D { X = px, Y = py });
                        }
                    }
                }
            }
            return points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        }

        public List<Point2D> GetWallOffPoints(List<Point2D> chokePoints)
        {
            var wallPoints = new List<Point2D>();
            foreach (var p in chokePoints)
            {
                if (_buildingService.AreaBuildable(p.X, p.Y, 1))
                {
                    wallPoints.Add(p);
                }
                else
                {
                    if (_buildingService.AreaBuildable(p.X, p.Y - 1, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X, p.Y + 1, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X - 1, p.Y, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X - 1, p.Y - 1, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X - 1, p.Y + 1, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X + 1, p.Y, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X + 1, p.Y - 1, 0.1f)) wallPoints.Add(p);
                    if (_buildingService.AreaBuildable(p.X + 1, p.Y + 1, 0.1f)) wallPoints.Add(p);
                }
            }
            return wallPoints.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        }

        private bool TouchingHigherPoint(int x, int y, int startHeight)
        {
            if (_mapDataService.MapHeightValue(x, y + 1) > startHeight && _mapDataService.PathWalkable(x, y + 1)) return true;
            if (_mapDataService.MapHeightValue(x, y - 1) > startHeight && _mapDataService.PathWalkable(x, y - 1)) return true;
            if (_mapDataService.MapHeightValue(x + 1, y) > startHeight && _mapDataService.PathWalkable(x + 1, y)) return true;
            if (_mapDataService.MapHeightValue(x - 1, y) > startHeight && _mapDataService.PathWalkable(x - 1, y)) return true;
            return false;
        }

        private bool TouchingLowerPoint(int x, int y, int startHeight)
        {
            if (_mapDataService.MapHeightValue(x, y + 1) < startHeight && _mapDataService.PathWalkable(x, y + 1)) return true;
            if (_mapDataService.MapHeightValue(x, y - 1) < startHeight && _mapDataService.PathWalkable(x, y - 1)) return true;
            if (_mapDataService.MapHeightValue(x + 1, y) < startHeight && _mapDataService.PathWalkable(x + 1, y)) return true;
            if (_mapDataService.MapHeightValue(x - 1, y) < startHeight && _mapDataService.PathWalkable(x - 1, y)) return true;
            return false;
        }
    }
}

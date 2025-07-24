using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Units;

namespace Utilities
{
    public static class PathingHelper
    {
        private static readonly Dictionary<string, List<Point2D>> _cachedPaths = new();

        public static float Distance(Point a, Point2D b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float Distance(Point2D a, Point2D b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static List<Point2D> CreateCircularPath(Point2D center, float radius, int segments, IEnumerable<Unit> unitsToAvoid = null, float avoidanceRadius = 1.5f)
        {
            var waypoints = new List<Point2D>();
            for (int i = 0; i < segments; i++)
            {
                float angle = 2 * MathF.PI * i / segments;
                float x = center.X + radius * MathF.Cos(angle);
                float y = center.Y + radius * MathF.Sin(angle);
                var point = new Point2D { X = x, Y = y };

                bool blocked = false;
                if (unitsToAvoid != null)
                {
                    foreach (var unit in unitsToAvoid)
                    {
                        if (unit.Alliance != Alliance.Enemy) continue;
                        if ((UnitType)unit.UnitType == UnitType.SCV) continue;

                        if (Distance(unit.Pos, point) < avoidanceRadius)
                        {
                            blocked = true;
                            break;
                        }
                    }
                }

                if (!blocked)
                    waypoints.Add(point);
            }

            return waypoints;
        }

        public static List<Point2D> GetOrCreateCircularPath(
            string key,
            Point2D center,
            float radius,
            int segments,
            IEnumerable<Unit> unitsToAvoid = null,
            float avoidanceRadius = 1.5f)
        {
            if (_cachedPaths.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var path = CreateCircularPath(center, radius, segments, unitsToAvoid, avoidanceRadius);
            _cachedPaths[key] = path;
            return path;
        }

        public static void ClearPath(string key) => _cachedPaths.Remove(key);

        public static void ClearAllPaths() => _cachedPaths.Clear();
    }
}

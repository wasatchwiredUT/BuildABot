using System;
using System.Collections.Generic;
using SC2APIProtocol;

namespace Utilities
{
    /// <summary>
    /// Simple A* path-finding implementation for SC2 pathing grids.
    /// This class exposes a FindPath method that takes a start and end
    /// coordinate and returns a list of waypoints representing a
    /// walkable path.  It is intentionally light‑weight and does not
    /// perform any caching.  It should be instantiated once and
    /// reused if possible.
    /// </summary>
    public class AStarPathfinder : IPathFinder
    {
        private readonly byte[] _pathData;
        private readonly int _width;
        private readonly int _height;

        /// <summary>
        /// Construct a path finder from the game information.  The
        /// StartRaw.PathingGrid data is copied into an internal buffer so
        /// subsequent calls to FindPath do not need to access the
        /// proto objects again.
        /// </summary>
        /// <param name="gameInfo">The game info received during OnStart.</param>
        public AStarPathfinder(ResponseGameInfo gameInfo)
        {
            var pathing = gameInfo.StartRaw.PathingGrid;
            _width = pathing.Size.X;
            _height = pathing.Size.Y;
            _pathData = pathing.Data.ToByteArray();
        }

        private bool IsWalkable(int x, int y)
        {
            // Discard coordinates that are outside the recorded width/height.
            if (x < 0 || y < 0 || x >= _width || y >= _height)
            {
                return false;
            }
            int index = x + y * _width;
            // Guard against a mismatch between width*height and _pathData.Length.
            if (index < 0 || index >= _pathData.Length)
            {
                return false;
            }
            // In SC2, 0 means unwalkable; any non-zero value (often 255) is walkable.
            return _pathData[index] != 0;
        }

        private bool TryGetNearestWalkable(int x, int y, int searchRadius, out (int, int) result)
        {
            if (IsWalkable(x, y))
            {
                result = (x, y);
                return true;
            }

            for (int r = 1; r <= searchRadius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue; // only check ring
                        int nx = x + dx;
                        int ny = y + dy;
                        if (IsWalkable(nx, ny))
                        {
                            result = (nx, ny);
                            return true;
                        }
                    }
                }
            }

            result = default;
            return false;
        }


        private static float Heuristic(int x1, int y1, int x2, int y2)
        {
            // Use Euclidean distance as the heuristic.
            int dx = x1 - x2;
            int dy = y1 - y2;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Compute a path between two points using A* search.  The returned
        /// list will include both the start and end points.  If no path
        /// exists, an empty list is returned.
        /// </summary>
        public List<Point2D> FindPath(Point2D start, Point2D end)
        {
            var result = new List<Point2D>();
            int sx = (int)Math.Round(start.X);
            int sy = (int)Math.Round(start.Y);
            int ex = (int)Math.Round(end.X);
            int ey = (int)Math.Round(end.Y);

            if (!TryGetNearestWalkable(sx, sy, 5, out var startTile))
            {
                // expand search radius in case the unit is surrounded by
                // unwalkable tiles such as structures.
                if (!TryGetNearestWalkable(sx, sy, 15, out startTile))
                {
                    return result;
                }
            }
            if (!TryGetNearestWalkable(ex, ey, 5, out var endTile))
            {
                if (!TryGetNearestWalkable(ex, ey, 15, out endTile))
                {
                    return result;
                }
            }

            var openSet = new PriorityQueue<Node, float>();
            var cameFrom = new Dictionary<(int, int), (int, int)>();
            var gScore = new Dictionary<(int, int), float>();

            var startPos = startTile;
            var endPos = endTile;

            // initialize the start node
            gScore[startPos] = 0;
            openSet.Enqueue(new Node(startPos.Item1, startPos.Item2), Heuristic(startPos.Item1, startPos.Item2, endPos.Item1, endPos.Item2));

            // neighbour movement (4 cardinal + 4 diagonal)
            int[][] directions = new[]
            {
                new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 },
                new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 }
            };

            var visited = new HashSet<(int, int)>();
            while (openSet.Count > 0)
            {
                // pop the node with lowest f-score
                openSet.TryDequeue(out Node currentNode, out _);
                var current = (currentNode.X, currentNode.Y);
                if (visited.Contains(current))
                {
                    continue;
                }
                visited.Add(current);

                // reached goal: reconstruct path
                if (current == endPos)
                {
                    var path = new List<(int, int)> { current };
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        path.Add(current);
                    }
                    path.Reverse();
                    foreach (var (px, py) in path)
                    {
                        // center waypoints in the tile by adding 0.5f
                        result.Add(new Point2D { X = px + 0.5f, Y = py + 0.5f });
                    }
                    return result;
                }

                // explore neighbours
                foreach (var dir in directions)
                {
                    int nx = currentNode.X + dir[0];
                    int ny = currentNode.Y + dir[1];
                    var neighbor = (nx, ny);
                    if (!IsWalkable(nx, ny) || visited.Contains(neighbor))
                    {
                        continue;
                    }
                    // cost is 1 for orthogonal moves, sqrt(2) ≈ 1.4142 for diagonals
                    float stepCost = (dir[0] != 0 && dir[1] != 0) ? 1.4142f : 1f;
                    float tentativeG = gScore[current] + stepCost;
                    if (!gScore.TryGetValue(neighbor, out float best) || tentativeG < best)
                    {
                        // found a better path to neighbour
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                    float fScore = tentativeG + Heuristic(nx, ny, endPos.Item1, endPos.Item2);
                        openSet.Enqueue(new Node(nx, ny), fScore);
                    }
                }
            }

            // no path found
            return result;
        }

        // simple record to represent nodes in the priority queue
        private record struct Node(int X, int Y);
    }
}

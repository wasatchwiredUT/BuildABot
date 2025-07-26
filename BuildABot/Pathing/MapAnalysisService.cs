// /BuildABot/Analysis/MapAnalysisService.cs

using SC2APIProtocol;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Pathing
    {
    /// <summary>
    /// A service to analyze the game map for strategic features like expansions,
    /// choke points, and ramps using professional ramp detection algorithms.
    /// </summary>
    public class MapAnalysisService
        {
        private readonly ResponseGameInfo _gameInfo;

        // Public lists to store the identified features
        public List<Point2D> ExpansionLocations
            {
            get; private set;
            }
        public List<Point2D> ChokePoints
            {
            get; private set;
            }
        public List<Point2D> Ramps
            {
            get; private set;
            }

        public MapAnalysisService(ResponseGameInfo gameInfo)
            {
            _gameInfo = gameInfo;
            ExpansionLocations = new List<Point2D>();
            ChokePoints = new List<Point2D>();
            Ramps = new List<Point2D>();
            }

        /// <summary>
        /// Runs the full map analysis suite.
        /// </summary>
        public void Analyze()
            {
            // The StartRaw data contains potential start locations, which are also expansions.
            ExpansionLocations.AddRange(_gameInfo.StartRaw.StartLocations);

            // Run ramp and choke analysis
            AnalyzeChokePointsAndRamps();
            }

        /// <summary>
        /// Analyzes terrain to find ramps using walkable-but-not-buildable detection.
        /// Enhanced with detailed diagnostics to debug main base ramp detection.
        /// </summary>
        private void AnalyzeChokePointsAndRamps()
            {
            var heightMap = _gameInfo.StartRaw.TerrainHeight;
            var pathingGrid = _gameInfo.StartRaw.PathingGrid;
            var placementGrid = _gameInfo.StartRaw.PlacementGrid;
            int width = heightMap.Size.X;
            int height = heightMap.Size.Y;

            Debug.WriteLine($"[MapAnalysis] Analyzing {width}x{height} terrain for ramp detection");

            // Step 1: Find all potential ramp cells (walkable but not buildable)
            var potentialRampCells = new List<Point2D>();

            for (int x = 1; x < width - 1; x++)
                {
                for (int y = 1; y < height - 1; y++)
                    {
                    bool isWalkable = IsPathable(x, y, width, pathingGrid.Data);
                    bool isBuildable = IsBuildable(x, y, width, placementGrid.Data);

                    // Ramps are walkable but not buildable
                    if (isWalkable && !isBuildable)
                        {
                        potentialRampCells.Add(new Point2D { X = x, Y = y });
                        }
                    }
                }

            Debug.WriteLine($"[MapAnalysis] Found {potentialRampCells.Count} potential ramp cells");

            // Let's see if there are any potential ramp cells near main bases
            foreach (var startLoc in _gameInfo.StartRaw.StartLocations)
                {
                var nearbyRampCells = potentialRampCells.Where(cell =>
                    System.Math.Abs(cell.X - startLoc.X) < 25 && System.Math.Abs(cell.Y - startLoc.Y) < 25).ToList();
                Debug.WriteLine($"[MapAnalysis] Near start location ({startLoc.X:F1}, {startLoc.Y:F1}): {nearbyRampCells.Count} potential ramp cells");
                }

            // Step 2: Cluster nearby ramp cells into ramp groups
            var rampClusters = ClusterRampCells(potentialRampCells);
            Debug.WriteLine($"[MapAnalysis] Clustered into {rampClusters.Count} ramp groups");

            // Step 3: Validate clusters as real ramps with detailed logging
            int validatedCount = 0;
            int rejectedCount = 0;

            foreach (var cluster in rampClusters)
                {
                var centerX = cluster.Average(p => p.X);
                var centerY = cluster.Average(p => p.Y);

                if (IsValidRamp(cluster, width, heightMap.Data))
                    {
                    var rampCenter = new Point2D { X = (float)centerX, Y = (float)centerY };
                    Ramps.Add(rampCenter);
                    validatedCount++;
                    Debug.WriteLine($"[MapAnalysis] ✓ Valid ramp {validatedCount} at ({rampCenter.X:F1}, {rampCenter.Y:F1}) with {cluster.Count} cells");
                    }
                else
                    {
                    rejectedCount++;
                    Debug.WriteLine($"[MapAnalysis] ✗ Rejected cluster {rejectedCount} at ({centerX:F1}, {centerY:F1}) with {cluster.Count} cells");

                    // Show why it was rejected
                    var heights = cluster.Select(p => GetHeight((int)p.X, (int)p.Y, width, heightMap.Data)).ToList();
                    var uniqueHeights = heights.Distinct().Count();
                    var minHeight = heights.Min();
                    var maxHeight = heights.Max();
                    var heightDiff = maxHeight - minHeight;

                    Debug.WriteLine($"    Rejection details: {cluster.Count} cells, {uniqueHeights} unique heights, {heightDiff} height diff");
                    }
                }

            // Simple choke point detection
            for (int x = 2; x < width - 2; x++)
                {
                for (int y = 2; y < height - 2; y++)
                    {
                    if (IsNarrowPassage(x, y, width, pathingGrid.Data))
                        {
                        if (!ChokePoints.Any(c => System.Math.Abs(c.X - x) < 8 && System.Math.Abs(c.Y - y) < 8))
                            {
                            ChokePoints.Add(new Point2D { X = x, Y = y });
                            }
                        }
                    }
                }

            Debug.WriteLine($"[MapAnalysis] Final result: {Ramps.Count} validated ramps, {rejectedCount} rejected clusters, {ChokePoints.Count} choke points");
            }

        /// <summary>
        /// Check if a position is buildable using the placement grid.
        /// </summary>
        private bool IsBuildable(int x, int y, int width, Google.Protobuf.ByteString placementData)
            {
            if (x < 0 || y < 0 || x >= width || y >= _gameInfo.StartRaw.PlacementGrid.Size.Y)
                return false;

            var byteIndex = (y * width + x) / 8;
            var bitIndex = (y * width + x) % 8;

            if (byteIndex >= placementData.Length)
                return false;

            return ((placementData[byteIndex] >> (7 - bitIndex)) & 1) == 1;
            }

        /// <summary>
        /// Cluster nearby ramp cells into connected groups using simple distance clustering.
        /// </summary>
        private List<List<Point2D>> ClusterRampCells(List<Point2D> rampCells)
            {
            var clusters = new List<List<Point2D>>();
            var processed = new HashSet<Point2D>();

            foreach (var cell in rampCells)
                {
                if (processed.Contains(cell))
                    continue;

                var cluster = new List<Point2D>();
                var toProcess = new Queue<Point2D>();
                toProcess.Enqueue(cell);

                while (toProcess.Count > 0)
                    {
                    var current = toProcess.Dequeue();
                    if (processed.Contains(current))
                        continue;

                    processed.Add(current);
                    cluster.Add(current);

                    // Find nearby cells (within 2 units)
                    var nearby = rampCells.Where(c =>
                        !processed.Contains(c) &&
                        System.Math.Abs(c.X - current.X) <= 2 &&
                        System.Math.Abs(c.Y - current.Y) <= 2);

                    foreach (var nearbyCell in nearby)
                        {
                        toProcess.Enqueue(nearbyCell);
                        }
                    }

                if (cluster.Count >= 3) // Minimum size for a ramp
                    {
                    clusters.Add(cluster);
                    }
                else if (cluster.Count > 0)
                    {
                    Debug.WriteLine($"[MapAnalysis] Discarded small cluster of {cluster.Count} cells at ({cluster[0].X:F1}, {cluster[0].Y:F1})");
                    }
                }

            return clusters;
            }

        /// <summary>
        /// Validate that a cluster of cells represents a real ramp.
        /// Relaxed criteria to catch real ramps that were being rejected.
        /// </summary>
        private bool IsValidRamp(List<Point2D> cluster, int width, Google.Protobuf.ByteString heightData)
            {
            if (cluster.Count < 4)
                {
                Debug.WriteLine($"    ✗ Too small: {cluster.Count} < 4 cells");
                return false;
                }

            // Get all heights in the cluster
            var heights = cluster.Select(p => GetHeight((int)p.X, (int)p.Y, width, heightData)).ToList();
            var uniqueHeights = heights.Distinct().Count();

            var minHeight = heights.Min();
            var maxHeight = heights.Max();
            var heightDifference = maxHeight - minHeight;

            // Much more lenient criteria based on actual data
            // Ramps can have even small height differences
            if (heightDifference < 8)
                {
                Debug.WriteLine($"    ✗ Height difference too small: {heightDifference} < 8");
                return false;
                }
            if (heightDifference > 100)
                {
                Debug.WriteLine($"    ✗ Height difference too large: {heightDifference} > 100 (cliff?)");
                return false;
                }

            // For small height differences, allow fewer unique heights
            int requiredHeights = heightDifference >= 16 ? 3 : 2;
            if (uniqueHeights < requiredHeights)
                {
                Debug.WriteLine($"    ✗ Not enough height levels: {uniqueHeights} < {requiredHeights} (for height diff {heightDifference})");
                return false;
                }

            // For small height differences, skip the progressive slope check
            if (heightDifference >= 16)
                {
                // Check for progressive height transition (not just two levels)
                var sortedHeights = heights.OrderBy(h => h).ToList();
                var middleHeights = sortedHeights.Skip(sortedHeights.Count / 4).Take(sortedHeights.Count / 2);

                // Should have intermediate heights between min and max
                var hasProgressiveSlope = middleHeights.Any(h => h > minHeight + heightDifference * 0.2 && h < maxHeight - heightDifference * 0.2);

                if (!hasProgressiveSlope)
                    {
                    Debug.WriteLine($"    ✗ No progressive slope: heights {minHeight}-{maxHeight}");
                    return false;
                    }
                }

            Debug.WriteLine($"    ✓ Valid: {cluster.Count} cells, {uniqueHeights} heights, {heightDifference} height diff");
            return true;
            }

        /// <summary>
        /// Check if a position is part of a narrow passage.
        /// </summary>
        private bool IsNarrowPassage(int x, int y, int width, Google.Protobuf.ByteString pathingData)
            {
            // Check if we're in a corridor that's 1-3 tiles wide
            int passageWidth = CalculatePassageWidth(x, y, width, pathingData);
            return passageWidth > 0 && passageWidth <= 3;
            }

        /// <summary>
        /// Calculate the width of a passage at a given point.
        /// </summary>
        private int CalculatePassageWidth(int x, int y, int width, Google.Protobuf.ByteString pathingData)
            {
            // Check horizontal corridor width
            int leftDist = 0, rightDist = 0;

            // Count tiles to the left
            for (int i = 1; i <= 10; i++)
                {
                if (x - i < 0 || !IsPathable(x - i, y, width, pathingData))
                    break;
                leftDist++;
                }

            // Count tiles to the right  
            for (int i = 1; i <= 10; i++)
                {
                if (x + i >= width || !IsPathable(x + i, y, width, pathingData))
                    break;
                rightDist++;
                }

            int horizontalWidth = leftDist + rightDist + 1;

            // Check vertical corridor width
            int upDist = 0, downDist = 0;

            // Count tiles up
            for (int i = 1; i <= 10; i++)
                {
                if (y - i < 0 || !IsPathable(x, y - i, width, pathingData))
                    break;
                upDist++;
                }

            // Count tiles down
            for (int i = 1; i <= 10; i++)
                {
                if (y + i >= _gameInfo.StartRaw.PathingGrid.Size.Y || !IsPathable(x, y + i, width, pathingData))
                    break;
                downDist++;
                }

            int verticalWidth = upDist + downDist + 1;

            // Return the smaller width (narrower direction)
            return System.Math.Min(horizontalWidth, verticalWidth);
            }

        private int GetHeight(int x, int y, int width, Google.Protobuf.ByteString data)
            {
            return data[y * width + x];
            }

        private bool IsPathable(int x, int y, int width, Google.Protobuf.ByteString data)
            {
            var byteIndex = (y * width + x) / 8;
            var bitIndex = x % 8;
            return ((data[byteIndex] >> (7 - bitIndex)) & 1) == 1;
            }

        /// <summary>
        /// Finds the nearest choke point to a given position.
        /// </summary>
        /// <param name="position">The position to search from</param>
        /// <param name="maxDistance">Maximum distance to search (optional)</param>
        /// <returns>The nearest choke point, or null if none found</returns>
        public Point2D FindNearestChokePoint(Point2D position, float maxDistance = float.MaxValue)
            {
            return ChokePoints
                .Where(choke => {
                    var distance = System.Math.Sqrt(
                        System.Math.Pow(choke.X - position.X, 2) +
                        System.Math.Pow(choke.Y - position.Y, 2));
                    return distance <= maxDistance;
                })
                .OrderBy(choke =>
                    System.Math.Pow(choke.X - position.X, 2) +
                    System.Math.Pow(choke.Y - position.Y, 2))
                .FirstOrDefault();
            }

        /// <summary>
        /// Finds the nearest ramp to a given position.
        /// </summary>
        /// <param name="position">The position to search from</param>
        /// <param name="maxDistance">Maximum distance to search (optional)</param>
        /// <returns>The nearest ramp, or null if none found</returns>
        public Point2D FindNearestRamp(Point2D position, float maxDistance = float.MaxValue)
            {
            return Ramps
                .Where(ramp => {
                    var distance = System.Math.Sqrt(
                        System.Math.Pow(ramp.X - position.X, 2) +
                        System.Math.Pow(ramp.Y - position.Y, 2));
                    return distance <= maxDistance;
                })
                .OrderBy(ramp =>
                    System.Math.Pow(ramp.X - position.X, 2) +
                    System.Math.Pow(ramp.Y - position.Y, 2))
                .FirstOrDefault();
            }

        /// <summary>
        /// Finds choke points that provide good defensive positions facing a particular direction.
        /// </summary>
        /// <param name="position">Current position</param>
        /// <param name="facingDirection">Direction to face (toward enemy)</param>
        /// <param name="maxDistance">Maximum search distance</param>
        /// <returns>Best defensive choke point, or null if none found</returns>
        public Point2D FindDefensiveChokePoint(Point2D position, Point2D facingDirection, float maxDistance = 25f)
            {
            var targetDirection = new
                {
                X = facingDirection.X - position.X,
                Y = facingDirection.Y - position.Y
                };
            var targetLength = System.Math.Sqrt(targetDirection.X * targetDirection.X + targetDirection.Y * targetDirection.Y);

            if (targetLength == 0)
                return FindNearestChokePoint(position, maxDistance);

            var normalizedTarget = new
                {
                X = targetDirection.X / targetLength,
                Y = targetDirection.Y / targetLength
                };

            return ChokePoints
                .Where(choke => {
                    var distance = System.Math.Sqrt(
                        System.Math.Pow(choke.X - position.X, 2) +
                        System.Math.Pow(choke.Y - position.Y, 2));
                    return distance <= maxDistance;
                })
                .OrderByDescending(choke => {
                    // Calculate alignment with facing direction
                    var chokeDirection = new
                        {
                        X = choke.X - position.X,
                        Y = choke.Y - position.Y
                        };
                    var chokeLength = System.Math.Sqrt(chokeDirection.X * chokeDirection.X + chokeDirection.Y * chokeDirection.Y);

                    if (chokeLength == 0)
                        return 0;

                    var normalizedChoke = new
                        {
                        X = chokeDirection.X / chokeLength,
                        Y = chokeDirection.Y / chokeLength
                        };

                    // Dot product for alignment
                    return normalizedChoke.X * normalizedTarget.X + normalizedChoke.Y * normalizedTarget.Y;
                })
                .ThenBy(choke =>
                    System.Math.Pow(choke.X - position.X, 2) +
                    System.Math.Pow(choke.Y - position.Y, 2))
                .FirstOrDefault();
            }
        }
    }
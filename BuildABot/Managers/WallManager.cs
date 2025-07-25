using SC2APIProtocol;
using Units;
using Pathing;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = SC2APIProtocol.Action;
using System.Diagnostics;

namespace Managers
    {
    /// <summary>
    /// Maintains a wall of structures at the main ramp using MapAnalysisService.
    /// Enhanced with two-phase SCV management: move first, then build.
    /// </summary>
    public class WallManager
        {
        private readonly ResponseGameInfo _gameInfo;
        private readonly MapAnalysisService _mapAnalysis;
        private readonly List<Point2D> _wallPositions = new();
        private bool _initialized;
        private Point2D _startLoc;
        private byte[] _placementData;
        private int _placeWidth, _placeHeight;

        // Two-phase building state management
        private readonly Dictionary<Point2D, BuildJob> _buildJobs = new();

        public WallManager(ResponseGameInfo gameInfo, MapAnalysisService mapAnalysis = null)
            {
            _gameInfo = gameInfo;

            // Create MapAnalysisService if not provided
            if (mapAnalysis == null)
                {
                _mapAnalysis = new MapAnalysisService(gameInfo);
                _mapAnalysis.Analyze(); // Run analysis immediately
                Debug.WriteLine($"[WallManager] Created and analyzed new MapAnalysisService");
                }
            else
                {
                _mapAnalysis = mapAnalysis;
                Debug.WriteLine($"[WallManager] Using provided MapAnalysisService");
                }

            // Extract placement data
            var placement = _gameInfo.StartRaw.PlacementGrid;
            _placementData = placement.Data.ToByteArray();
            _placeWidth = placement.Size.X;
            _placeHeight = placement.Size.Y;

            Debug.WriteLine($"[WallManager] Placement grid: {_placeWidth}x{_placeHeight}, data length: {_placementData.Length}");
            }

        /// <summary>
        /// Represents a building job with SCV state tracking.
        /// </summary>
        private class BuildJob
            {
            public Point2D Position
                {
                get; set;
                }
            public UnitType StructureType
                {
                get; set;
                }
            public ulong SCVTag
                {
                get; set;
                }
            public BuildState State
                {
                get; set;
                }
            public int LastUpdateFrame
                {
                get; set;
                }

            public BuildJob(Point2D position, UnitType structureType)
                {
                Position = position;
                StructureType = structureType;
                State = BuildState.NeedingSCV;
                LastUpdateFrame = 0;
                }
            }

        private enum BuildState
            {
            NeedingSCV,     // Need to assign an SCV
            MovingToSite,   // SCV is moving to build location
            ReadyToBuild,   // SCV arrived, ready to build
            Building,       // Build command issued
            Complete        // Structure exists
            }

        /// <summary>
        /// Compute wall positions based on MapAnalysisService data.
        /// </summary>
        public void Initialize(ResponseObservation observation)
            {
            if (_initialized)
                return;

            _startLoc = FindStartLocation(observation);
            if (_startLoc == null)
                return;

            Debug.WriteLine($"[WallManager] Main base at ({_startLoc.X:F1}, {_startLoc.Y:F1})");

            // Always show what ramps were detected for debugging
            if (_mapAnalysis?.Ramps?.Count > 0)
                {
                Debug.WriteLine($"[WallManager] Found {_mapAnalysis.Ramps.Count} ramps total");

                // Show detailed view of each ramp
                DrawRampDetails();

                // Draw area around our main base to see terrain
                int baseX = (int)_startLoc.X;
                int baseY = (int)_startLoc.Y;
                Debug.WriteLine($"[WallManager] Terrain around main base:");
                DrawMapToConsole(baseX - 15, baseY - 15, 30, 30);
                }
            else
                {
                Debug.WriteLine("[WallManager] No ramps detected by MapAnalysisService!");
                // Still draw the area to see what terrain looks like
                int baseX = (int)_startLoc.X;
                int baseY = (int)_startLoc.Y;
                DrawMapToConsole(baseX - 10, baseY - 10, 20, 20);
                }

            // Find the best ramp for walling
            var bestRamp = FindBestWallRamp();
            if (bestRamp == null)
                {
                Debug.WriteLine("[WallManager] No suitable ramp found near main base");
                return;
                }

            // Create wall positions around the ramp
            CreateWallPositions(bestRamp);

            _initialized = true;
            Debug.WriteLine($"[WallManager] Initialized with {_wallPositions.Count} wall positions");
            }

        /// <summary>
        /// Main method to maintain the wall each frame using two-phase building.
        /// </summary>
        public List<Action> MaintainWall(ResponseObservation observation, List<Unit> ourUnits)
            {
            if (!_initialized)
                Initialize(observation);

            List<Action> actions = new();
            if (_wallPositions.Count == 0)
                return actions;

            int currentFrame = (int)observation.Observation.GameLoop;
            int minerals = (int)observation.Observation.PlayerCommon.Minerals;

            // Create build jobs for positions that need structures
            CreateBuildJobs(ourUnits, minerals, currentFrame);

            // Process existing build jobs
            ProcessBuildJobs(ourUnits, actions, currentFrame);

            Debug.WriteLine($"[WallManager] Frame {currentFrame}: {_buildJobs.Count} active jobs, {actions.Count} actions, {minerals} minerals");
            return actions;
            }

        /// <summary>
        /// Create build jobs for wall positions that need structures.
        /// Only create jobs when we have sufficient resources.
        /// </summary>
        private void CreateBuildJobs(List<Unit> ourUnits, int minerals, int currentFrame)
            {
            foreach (var position in _wallPositions)
                {
                // Skip if we already have a job for this position
                if (_buildJobs.ContainsKey(position))
                    continue;

                // Skip if structure already exists
                if (HasStructureAt(position, ourUnits))
                    continue;

                // Only create job if we have enough minerals AND no other jobs are pending resources
                int pendingJobs = _buildJobs.Count(kvp => kvp.Value.State == BuildState.NeedingSCV || kvp.Value.State == BuildState.MovingToSite);
                int requiredMinerals = (pendingJobs + 1) * 100; // Cost for pending jobs + this new job

                if (minerals < requiredMinerals)
                    {
                    Debug.WriteLine($"[WallManager] Need {requiredMinerals} minerals for jobs, only have {minerals}");
                    continue;
                    }

                // Create new build job
                var job = new BuildJob(position, UnitType.SupplyDepot);
                job.LastUpdateFrame = currentFrame;
                _buildJobs[position] = job;

                Debug.WriteLine($"[WallManager] Created build job for ({position.X:F1}, {position.Y:F1}) at frame {currentFrame} (minerals: {minerals})");
                break; // Only create one job at a time
                }
            }

        /// <summary>
        /// Process all active build jobs based on their current state.
        /// </summary>
        private void ProcessBuildJobs(List<Unit> ourUnits, List<Action> actions, int currentFrame)
            {
            var jobsToRemove = new List<Point2D>();

            foreach (var kvp in _buildJobs)
                {
                var position = kvp.Key;
                var job = kvp.Value;

                Debug.WriteLine($"[WallManager] Processing job at ({position.X:F1}, {position.Y:F1}) - State: {job.State}, LastUpdate: {job.LastUpdateFrame}, CurrentFrame: {currentFrame}");

                // Check if structure was completed
                if (HasStructureAt(position, ourUnits))
                    {
                    Debug.WriteLine($"[WallManager] Structure completed at ({position.X:F1}, {position.Y:F1})");
                    jobsToRemove.Add(position);
                    continue;
                    }

                // Process job based on current state
                switch (job.State)
                    {
                    case BuildState.NeedingSCV:
                        ProcessNeedingSCV(job, ourUnits, actions, currentFrame);
                        break;

                    case BuildState.MovingToSite:
                        ProcessMovingToSite(job, ourUnits, actions, currentFrame);
                        break;

                    case BuildState.ReadyToBuild:
                        ProcessReadyToBuild(job, ourUnits, actions, currentFrame);
                        break;

                    case BuildState.Building:
                        ProcessBuilding(job, ourUnits, currentFrame);
                        break;
                    }

                // Remove stale jobs (SCV died or got reassigned) - but give them time to work
                int timeSinceLastUpdate = currentFrame - job.LastUpdateFrame;
                if (timeSinceLastUpdate > 448) // 20 seconds timeout instead of 10
                    {
                    Debug.WriteLine($"[WallManager] Removing stale job at ({position.X:F1}, {position.Y:F1}) - {timeSinceLastUpdate} frames since last update");
                    jobsToRemove.Add(position);
                    }
                }

            // Clean up completed/failed jobs
            foreach (var position in jobsToRemove)
                {
                _buildJobs.Remove(position);
                }
            }

        /// <summary>
        /// Process a job that needs an SCV assigned.
        /// </summary>
        private void ProcessNeedingSCV(BuildJob job, List<Unit> ourUnits, List<Action> actions, int currentFrame)
            {
            Debug.WriteLine($"[WallManager] Looking for SCV to assign to job at ({job.Position.X:F1}, {job.Position.Y:F1})");

            var builder = FindBuilder(ourUnits);
            if (builder == null)
                {
                Debug.WriteLine($"[WallManager] No SCV found for job at ({job.Position.X:F1}, {job.Position.Y:F1})");
                return;
                }

            // Assign SCV and order it to move to build site
            job.SCVTag = builder.Tag;
            job.State = BuildState.MovingToSite;
            job.LastUpdateFrame = currentFrame;

            var moveAction = builder.Move(job.Position);
            actions.Add(moveAction);

            Debug.WriteLine($"[WallManager] Assigned SCV {job.SCVTag} to move to ({job.Position.X:F1}, {job.Position.Y:F1})");
            }

        /// <summary>
        /// Process a job where SCV is moving to the build site.
        /// </summary>
        private void ProcessMovingToSite(BuildJob job, List<Unit> ourUnits, List<Action> actions, int currentFrame)
            {
            var scvUnit = ourUnits.FirstOrDefault(u => u.Tag == job.SCVTag);
            if (scvUnit == null)
                {
                Debug.WriteLine($"[WallManager] SCV {job.SCVTag} no longer exists, resetting job");
                job.State = BuildState.NeedingSCV;
                return;
                }

            float distance = Distance(scvUnit.Pos, job.Position);
            Debug.WriteLine($"[WallManager] SCV {job.SCVTag} moving to build site, distance: {distance:F1}");

            // Check if SCV arrived at build site
            if (distance < 3.0f)
                {
                job.State = BuildState.ReadyToBuild;
                job.LastUpdateFrame = currentFrame;
                Debug.WriteLine($"[WallManager] SCV {job.SCVTag} arrived at build site");
                }
            // Check if SCV got stuck or stopped moving
            else if (scvUnit.Orders.Count == 0 && distance > 5.0f)
                {
                Debug.WriteLine($"[WallManager] SCV {job.SCVTag} stopped moving, re-issuing move command");
                var builder = new SCV(scvUnit);
                var moveAction = builder.Move(job.Position);
                actions.Add(moveAction);
                }

            job.LastUpdateFrame = currentFrame;
            }

        /// <summary>
        /// Process a job where SCV is ready to build.
        /// </summary>
        private void ProcessReadyToBuild(BuildJob job, List<Unit> ourUnits, List<Action> actions, int currentFrame)
            {
            var scvUnit = ourUnits.FirstOrDefault(u => u.Tag == job.SCVTag);
            if (scvUnit == null)
                {
                Debug.WriteLine($"[WallManager] SCV {job.SCVTag} no longer exists, resetting job");
                job.State = BuildState.NeedingSCV;
                return;
                }

            // Issue build command
            var builder = new SCV(scvUnit);
            var buildAction = builder.BuildStructure(job.StructureType, job.Position);
            actions.Add(buildAction);

            job.State = BuildState.Building;
            job.LastUpdateFrame = currentFrame;

            Debug.WriteLine($"[WallManager] Issued build command for SCV {job.SCVTag} at ({job.Position.X:F1}, {job.Position.Y:F1})");
            }

        /// <summary>
        /// Process a job where build command has been issued.
        /// </summary>
        private void ProcessBuilding(BuildJob job, List<Unit> ourUnits, int currentFrame)
            {
            var scvUnit = ourUnits.FirstOrDefault(u => u.Tag == job.SCVTag);
            if (scvUnit == null)
                {
                Debug.WriteLine($"[WallManager] SCV {job.SCVTag} no longer exists during building");
                return;
                }

            Debug.WriteLine($"[WallManager] SCV {job.SCVTag} building, orders: {scvUnit.Orders.Count}");

            job.LastUpdateFrame = currentFrame;
            }

        // [Keep all your existing helper methods: DrawRampDetails, DrawMapToConsole, FindBestWallRamp, etc.]

        /// <summary>
        /// Draw a detailed view around each detected ramp.
        /// </summary>
        private void DrawRampDetails()
            {
            if (_mapAnalysis?.Ramps == null || !_mapAnalysis.Ramps.Any())
                {
                Debug.WriteLine("[WallManager] No ramps to visualize");
                return;
                }

            Debug.WriteLine($"\nDetailed view of {_mapAnalysis.Ramps.Count} detected ramps:");

            for (int i = 0; i < _mapAnalysis.Ramps.Count; i++)
                {
                var ramp = _mapAnalysis.Ramps[i];
                int centerX = (int)ramp.X;
                int centerY = (int)ramp.Y;

                Debug.WriteLine($"\nRamp {i + 1} at ({centerX}, {centerY}):");

                // Show 7x7 area around each ramp
                for (int dy = -3; dy <= 3; dy++)
                    {
                    string line = "";
                    for (int dx = -3; dx <= 3; dx++)
                        {
                        int x = centerX + dx;
                        int y = centerY + dy;

                        if (x >= 0 && x < _gameInfo.StartRaw.TerrainHeight.Size.X &&
                            y >= 0 && y < _gameInfo.StartRaw.TerrainHeight.Size.Y)
                            {
                            bool isPathable = IsPathable(x, y);
                            int height = GetTerrainHeight(x, y);

                            if (dx == 0 && dy == 0)
                                {
                                line += "*"; // Mark center
                                }
                            else if (!isPathable)
                                {
                                line += "#";
                                }
                            else
                                {
                                // Show last digit of height
                                line += (height % 10).ToString();
                                }
                            }
                        else
                            {
                            line += "?";
                            }
                        }
                    Debug.WriteLine(line);
                    }
                }
            }

        /// <summary>
        /// Draw a larger map view showing ramps and terrain.
        /// </summary>
        private void DrawMapToConsole(int startX, int startY, int mapwidth, int mapheight)
            {
            var heightMap = _gameInfo.StartRaw.TerrainHeight;
            var pathingGrid = _gameInfo.StartRaw.PathingGrid;
            int mapWidth = heightMap.Size.X;
            int mapHeight = heightMap.Size.Y;

            // Clamp the view to map bounds
            int endX = Math.Min(startX + mapwidth, mapWidth);
            int endY = Math.Min(startY + mapheight, mapHeight);

            Debug.WriteLine($"Map view from ({startX},{startY}) to ({endX},{endY}):");
            Debug.WriteLine("Legend: # = blocked, R = ramp, B = base, numbers = height/10");

            for (int y = startY; y < endY; y++)
                {
                string line = "";
                for (int x = startX; x < endX; x++)
                    {
                    // Check if this is our base location
                    if (_startLoc != null && Math.Abs(x - _startLoc.X) <= 1 && Math.Abs(y - _startLoc.Y) <= 1)
                        {
                        line += "B";
                        }
                    // Check if this position is a detected ramp
                    else if (_mapAnalysis?.Ramps?.Any(r => Math.Abs(r.X - x) < 2 && Math.Abs(r.Y - y) < 2) == true)
                        {
                        line += "R";
                        }
                    else if (IsPathable(x, y))
                        {
                        int heightb = GetTerrainHeight(x, y);
                        int heightDigit = (heightb / 10) % 10; // Show height as single digit
                        line += heightDigit.ToString();
                        }
                    else
                        {
                        line += "#";
                        }
                    }
                Debug.WriteLine($"{line} {y}");
                }

            // Show X coordinates
            string xCoords = " ";
            for (int x = startX; x < endX; x++)
                {
                xCoords += (x % 10).ToString();
                }
            Debug.WriteLine(xCoords);
            }

        /// <summary>
        /// Helper to check if a position is pathable.
        /// </summary>
        private bool IsPathable(int x, int y)
            {
            var pathingGrid = _gameInfo.StartRaw.PathingGrid;
            int width = pathingGrid.Size.X;
            int height = pathingGrid.Size.Y;

            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;

            var byteIndex = (y * width + x) / 8;
            var bitIndex = (y * width + x) % 8;
            return ((pathingGrid.Data[byteIndex] >> (7 - bitIndex)) & 1) == 1;
            }

        /// <summary>
        /// Helper to get terrain height.
        /// </summary>
        private int GetTerrainHeight(int x, int y)
            {
            var heightMap = _gameInfo.StartRaw.TerrainHeight;
            int width = heightMap.Size.X;

            if (x < 0 || y < 0 || x >= width || y >= heightMap.Size.Y)
                return 0;

            return heightMap.Data[y * width + x];
            }

        /// <summary>
        /// Find the best ramp for creating a wall.
        /// </summary>
        private Point2D FindBestWallRamp()
            {
            if (_mapAnalysis.Ramps == null || !_mapAnalysis.Ramps.Any())
                {
                Debug.WriteLine("[WallManager] No ramps found by MapAnalysisService");
                return null;
                }

            Debug.WriteLine($"[WallManager] Main base at ({_startLoc.X:F1}, {_startLoc.Y:F1})");
            Debug.WriteLine($"[WallManager] Found {_mapAnalysis.Ramps.Count} total ramps");

            // Only consider ramps that are reasonably close to our main base (within 20 units)
            var nearbyRamps = _mapAnalysis.Ramps
                .Where(ramp => Math.Sqrt(DistanceSquared(_startLoc, ramp)) < 20f)
                .ToList();

            if (!nearbyRamps.Any())
                {
                Debug.WriteLine("[WallManager] No ramps found within 20 units of main base");
                return null;
                }

            Debug.WriteLine($"[WallManager] Found {nearbyRamps.Count} ramps near main base");

            // Find the closest ramp among the nearby ones
            var bestRamp = nearbyRamps
                .OrderBy(ramp => DistanceSquared(_startLoc, ramp))
                .First();

            Debug.WriteLine($"[WallManager] Selected ramp at ({bestRamp.X:F1}, {bestRamp.Y:F1}), distance: {Math.Sqrt(DistanceSquared(_startLoc, bestRamp)):F1}");

            return bestRamp;
            }

        /// <summary>
        /// Create wall positions around the selected ramp.
        /// </summary>
        private void CreateWallPositions(Point2D rampCenter)
            {
            // Direction from ramp toward our base
            float dirX = _startLoc.X - rampCenter.X;
            float dirY = _startLoc.Y - rampCenter.Y;
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len == 0)
                len = 1f;
            dirX /= len;
            dirY /= len;

            // Perpendicular direction for spread
            float perpX = -dirY, perpY = dirX;

            // Create 3 wall positions: center, left, right
            for (int i = -1; i <= 1; i++)
                {
                float px = rampCenter.X + dirX * 2f + perpX * i * 3f;
                float py = rampCenter.Y + dirY * 2f + perpY * i * 3f;
                Point2D pos = FindNearestBuildable(px, py);
                if (pos != null)
                    {
                    _wallPositions.Add(pos);
                    Debug.WriteLine($"[WallManager] Added wall position at ({pos.X:F1}, {pos.Y:F1})");
                    }
                }
            }

        /// <summary>
        /// Check if a position is buildable using the placement grid.
        /// </summary>
        private bool IsBuildable(int x, int y)
            {
            if (x < 0 || y < 0 || x >= _placeWidth || y >= _placeHeight)
                return false;

            int cellIndex = y * _placeWidth + x;
            int byteIndex = cellIndex / 8;
            int bitIndex = cellIndex % 8;

            if (byteIndex >= _placementData.Length)
                return false;

            return (_placementData[byteIndex] & (1 << (7 - bitIndex))) != 0;
            }

        /// <summary>
        /// Find the nearest buildable position to the given coordinates.
        /// </summary>
        private Point2D FindNearestBuildable(float x, float y)
            {
            int ix = (int)Math.Round(x);
            int iy = (int)Math.Round(y);
            int maxRadius = 8;

            for (int r = 0; r <= maxRadius; r++)
                {
                for (int dx = -r; dx <= r; dx++)
                    {
                    for (int dy = -r; dy <= r; dy++)
                        {
                        int px = ix + dx;
                        int py = iy + dy;

                        if (IsBuildable(px, py))
                            {
                            return new Point2D { X = px + 0.5f, Y = py + 0.5f };
                            }
                        }
                    }
                }

            return null;
            }

        /// <summary>
        /// Find our starting command center location.
        /// </summary>
        private Point2D FindStartLocation(ResponseObservation observation)
            {
            foreach (var unit in observation.Observation.RawData.Units)
                {
                if (unit.Alliance == Alliance.Self &&
                    ((UnitType)unit.UnitType == UnitType.CommandCenter ||
                     (UnitType)unit.UnitType == UnitType.OrbitalCommand ||
                     (UnitType)unit.UnitType == UnitType.PlanetaryFortress))
                    {
                    return new Point2D { X = unit.Pos.X, Y = unit.Pos.Y };
                    }
                }

            return _gameInfo.StartRaw.StartLocations.Count > 0 ?
                _gameInfo.StartRaw.StartLocations[0] : null;
            }

        /// <summary>
        /// Find an available SCV for building.
        /// Prioritizes idle SCVs, then gathering SCVs (not returning with cargo).
        /// </summary>
        private SCV FindBuilder(List<Unit> units)
            {
            var scvs = units.Where(u => (UnitType)u.UnitType == UnitType.SCV).ToList();
            Debug.WriteLine($"[WallManager] Found {scvs.Count} total SCVs");

            // First try to find an idle SCV
            foreach (var unit in scvs)
                {
                if (unit.Orders.Count == 0)
                    {
                    Debug.WriteLine($"[WallManager] Found idle SCV {unit.Tag} at ({unit.Pos.X:F1}, {unit.Pos.Y:F1})");
                    return new SCV(unit);
                    }
                }

            // Second, try SCVs that are moving (safe to interrupt)
            foreach (var unit in scvs)
                {
                if (unit.Orders.Count > 0)
                    {
                    var order = unit.Orders[0];
                    if (order.AbilityId == (uint)AbilityId.Move)
                        {
                        Debug.WriteLine($"[WallManager] Found moving SCV {unit.Tag} at ({unit.Pos.X:F1}, {unit.Pos.Y:F1})");
                        return new SCV(unit);
                        }
                    }
                }

            // Third, interrupt SCVs that are gathering (295) - they're not carrying minerals
            foreach (var unit in scvs)
                {
                if (unit.Orders.Count > 0)
                    {
                    var order = unit.Orders[0];
                    if (order.AbilityId == 295) // HARVEST_GATHER - safe to interrupt
                        {
                        Debug.WriteLine($"[WallManager] Interrupting gathering SCV {unit.Tag} (no cargo loss)");
                        return new SCV(unit);
                        }
                    }
                }

            // Log what SCVs are doing if we can't find one
            Debug.WriteLine("[WallManager] No safe SCV found. Current SCV states:");
            foreach (var unit in scvs.Take(5)) // Show first 5 for debugging
                {
                var orderInfo = unit.Orders.Count > 0 ? unit.Orders[0].AbilityId.ToString() : "idle";
                Debug.WriteLine($"  SCV {unit.Tag}: {orderInfo}");
                }

            Debug.WriteLine($"[WallManager] No safe SCV available (avoiding cargo-carrying SCVs)");
            return null;
            }

        /// <summary>
        /// Calculate squared distance between two points.
        /// </summary>
        private float DistanceSquared(Point2D a, Point2D b)
            {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
            }

        /// <summary>
        /// Calculate distance between a Point and Point2D.
        /// </summary>
        private float Distance(Point pos, Point2D target)
            {
            float dx = pos.X - target.X;
            float dy = pos.Y - target.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
            }

        /// <summary>
        /// Check if a structure exists at the given position.
        /// </summary>
        private bool HasStructureAt(Point2D position, List<Unit> units)
            {
            foreach (var unit in units)
                {
                UnitType type = (UnitType)unit.UnitType;
                if ((type == UnitType.SupplyDepot ||
                     type == UnitType.SupplyDepotLowered ||
                     type == UnitType.Bunker ||
                     type == UnitType.Barracks) &&
                    Distance(unit.Pos, position) < 1.5f)
                    {
                    return true;
                    }
                }
            return false;
            }

        /// <summary>
        /// Get the main ramp position for defensive positioning.
        /// </summary>
        public Point2D GetMainRampPosition()
            {
            if (!_initialized || _wallPositions.Count == 0)
                return null;

            float avgX = _wallPositions.Average(p => p.X);
            float avgY = _wallPositions.Average(p => p.Y);
            return new Point2D { X = avgX, Y = avgY };
            }

        /// <summary>
        /// Get all wall positions.
        /// </summary>
        public IReadOnlyList<Point2D> GetWallPositions()
            {
            return _wallPositions.AsReadOnly();
            }
        }
    }
using SC2APIProtocol;
using System;
using System.Collections.Generic;

namespace Pathing
{
    public class MapDataService
    {
        private readonly byte[] _pathing;
        private readonly byte[] _placement;
        private readonly byte[] _terrain;
        private readonly int _width;
        private readonly int _height;

        public MapDataService(ResponseGameInfo gameInfo)
        {
            var start = gameInfo.StartRaw;
            _pathing = start.PathingGrid.Data.ToByteArray();
            _placement = start.PlacementGrid.Data.ToByteArray();
            _terrain = start.TerrainHeight.Data.ToByteArray();
            _width = start.PathingGrid.Size.X;
            _height = start.PathingGrid.Size.Y;
        }

        public int MapWidth => _width;
        public int MapHeight => _height;

        private int Index(int x, int y) => x + y * _width;

        public bool PathWalkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return false;
            int i = Index(x, y);
            if (i < 0 || i >= _pathing.Length) return false;
            return _pathing[i] != 0;
        }

        public bool PathWalkable(float x, float y) => PathWalkable((int)MathF.Round(x), (int)MathF.Round(y));
        public bool PathWalkable(Point2D p) => PathWalkable(p.X, p.Y);

        public bool PathBuildable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return false;
            int i = Index(x, y);
            if (i < 0 || i >= _placement.Length) return false;
            return _placement[i] != 0;
        }

        public bool PathBuildable(float x, float y) => PathBuildable((int)MathF.Round(x), (int)MathF.Round(y));
        public bool PathBuildable(Point2D p) => PathBuildable(p.X, p.Y);

        public int MapHeightValue(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return 0;
            int i = Index(x, y);
            if (i < 0 || i >= _terrain.Length) return 0;
            return _terrain[i];
        }

        public int GetMapHeight(Point2D p) => MapHeightValue((int)MathF.Round(p.X), (int)MathF.Round(p.Y));

        public List<MapCell> GetCells(float x, float y, float radius)
        {
            var cells = new List<MapCell>();
            int xMin = (int)Math.Floor(x - radius);
            int xMax = (int)Math.Ceiling(x + radius);
            int yMin = (int)Math.Floor(y - radius);
            int yMax = (int)Math.Ceiling(y + radius);
            for (int ix = xMin; ix <= xMax; ix++)
            {
                for (int iy = yMin; iy <= yMax; iy++)
                {
                    if (ix < 0 || iy < 0 || ix >= _width || iy >= _height) continue;
                    cells.Add(new MapCell
                    {
                        X = ix,
                        Y = iy,
                        TerrainHeight = MapHeightValue(ix, iy),
                        Walkable = PathWalkable(ix, iy),
                        Buildable = PathBuildable(ix, iy)
                    });
                }
            }
            return cells;
        }
    }
}

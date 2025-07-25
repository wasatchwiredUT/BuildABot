using System;

namespace Pathing
{
    public class BuildingService
    {
        private readonly MapDataService _mapData;

        public BuildingService(MapDataService mapData)
        {
            _mapData = mapData;
        }

        public bool AreaBuildable(float x, float y, float radius)
        {
            int minX = (int)Math.Floor(x - radius);
            int maxX = (int)Math.Ceiling(x + radius);
            int minY = (int)Math.Floor(y - radius);
            int maxY = (int)Math.Ceiling(y + radius);

            for (int ix = minX; ix <= maxX; ix++)
            {
                for (int iy = minY; iy <= maxY; iy++)
                {
                    if (!_mapData.PathBuildable(ix, iy))
                        return false;
                }
            }
            return true;
        }
    }
}

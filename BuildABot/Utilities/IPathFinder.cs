using System.Collections.Generic;
using SC2APIProtocol;

namespace Utilities
{
    /// <summary>
    /// Simple path finding interface used by pathing utilities.
    /// </summary>
    public interface IPathFinder
    {
        /// <summary>
        /// Compute a ground path between the given points.
        /// </summary>
        List<Point2D> FindPath(Point2D start, Point2D end);
    }
}

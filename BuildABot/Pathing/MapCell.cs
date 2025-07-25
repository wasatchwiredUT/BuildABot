namespace Pathing
{
    public class MapCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int TerrainHeight { get; set; }
        public bool Walkable { get; set; }
        public bool Buildable { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace WorldEdit
{
    public static class ID
    {
        public static List<int> GetColorID(string color)
        {
            if (int.TryParse(color, out var id) && id >= 0 && id < Main.numTileColors)
                return new List<int> { id };

            var list = new List<int>();
            foreach (var kvp in WorldEdit.Colors)
            {
                if (kvp.Key == color)
                    return new List<int> { kvp.Value };
                if (kvp.Key.StartsWith(color))
                    list.Add(kvp.Value);
            }
            return list;
        }

        public static List<int> GetCoatID(string coat)
        {
            if (int.TryParse(coat, out var id) && id >= 0 && id < 2)
                return new List<int> { id };

            var list = new List<int>();
            foreach (var kvp in WorldEdit.Coats)
            {
                if (kvp.Key == coat)
                    return new List<int> { kvp.Value };
                if (kvp.Key.StartsWith(coat))
                    list.Add(kvp.Value);
            }
            return list;
        }

        public static List<int> GetTileID(string tile)
        {
            if (int.TryParse(tile, out var id) && id >= 0 && id < Main.maxTileSets)
                return new List<int> { id };

            var list = new List<int>();
            foreach (var kvp in WorldEdit.Tiles)
            {
                if (kvp.Key == tile)
                    return new List<int> { kvp.Value };
                if (kvp.Key.StartsWith(tile))
                    list.Add(kvp.Value);
            }
            return list;
        }

        public static List<int> GetWallID(string wall)
        {
            if (int.TryParse(wall, out var id) && id >= 0 && id < Main.maxWallTypes)
                return new List<int> { id };

            var list = new List<int>();
            foreach (var kvp in WorldEdit.Walls)
            {
                if (kvp.Key == wall)
                    return new List<int> { kvp.Value };
                if (kvp.Key.StartsWith(wall))
                    list.Add(kvp.Value);
            }
            return list;
        }

        public static int GetSlopeID(string slope)
        {
            if (int.TryParse(slope, out var id) && id >= 0 && id < 6)
                return id;

            if (!WorldEdit.Slopes.TryGetValue(slope, out int Slope))
                return -1;

            return Slope;
        }
    }
}

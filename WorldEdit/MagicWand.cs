using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit
{
    public class MagicWand
    {
        public static int MaxPointCount { get; set; }

        internal bool DontCheck = false;
        internal List<WEPoint> Points = new();

        public MagicWand() 
            => DontCheck = true;

        public MagicWand(WEPoint[] Points)
        {
            this.DontCheck = false;
            this.Points = Points?.ToList() ?? new List<WEPoint>();
        }

        public bool InSelection(int X, int Y) =>
            DontCheck || Points.Any(p => p.X == X && p.Y == Y);
        
        public static bool GetMagicWandSelection(int X, int Y, Expression expression,
            TSPlayer player, out MagicWand wand)
        {
            wand = new MagicWand();

            if (!Tools.InMapBoundaries(X, Y) || (expression == null))
                return false;
            if (!expression.Evaluate(Main.tile[X, Y], X, Y))
                return false;

            short x = (short)X, y = (short)Y;

            List<WEPoint> WEPoints = new() { new WEPoint(x, y) };

            int index = 0, count = 0;
            bool[,] was = new bool[Main.maxTilesX, Main.maxTilesY];

            was[x, y] = true;
            while (index < WEPoints.Count)
            {
                WEPoint point = WEPoints[index];
                foreach (WEPoint p in new WEPoint[]
                {
                    new WEPoint((short)(point.X + 1), point.Y),
                    new WEPoint((short)(point.X - 1), point.Y),
                    new WEPoint(point.X, (short)(point.Y + 1)),
                    new WEPoint(point.X, (short)(point.Y - 1))
                })
                {
                    if (Tools.InMapBoundaries(p.X, p.Y) && !was[p.X, p.Y])
                    {
                        if (expression.Evaluate(Main.tile[p.X, p.Y], p.X, p.Y))
                        {
                            WEPoints.Add(p);
                            count++;
                        }
                        was[p.X, p.Y] = true;
                    }
                }
                if (count >= MaxPointCount)
                {
                    player.SendErrorMessage("Hard selection tile limit " +
                        $"({MaxPointCount}) has been reached.");
                    return false;
                }
                index++;
            }

            wand = new MagicWand(WEPoints.ToArray());
            return true;
        }
    }

    public struct WEPoint
    {
        public short X { get; set; }
        public short Y { get; set; }
        public WEPoint(short X, short Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
}
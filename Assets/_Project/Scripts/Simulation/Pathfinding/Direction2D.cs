namespace Nova.Simulation.Pathfinding
{
    public enum Direction2D : byte
    {
        None = 0,
        North = 1,
        NorthEast = 2,
        East = 3,
        SouthEast = 4,
        South = 5,
        SouthWest = 6,
        West = 7,
        NorthWest = 8
    }

    public static class Direction2DUtility
    {
        public static readonly Direction2D[] AllCardinalAndDiagonal = new Direction2D[]
        {
            Direction2D.North,
            Direction2D.NorthEast,
            Direction2D.East,
            Direction2D.SouthEast,
            Direction2D.South,
            Direction2D.SouthWest,
            Direction2D.West,
            Direction2D.NorthWest
        };

        private static readonly sbyte[] OffsetX = new sbyte[] { 0, 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly sbyte[] OffsetY = new sbyte[] { 0, 1, 1, 0, -1, -1, -1, 0, 1 };

        public static (sbyte dx, sbyte dy) GetOffset(Direction2D dir)
        {
            byte index = (byte)dir;
            if (index >= OffsetX.Length) return (0, 0);
            return (OffsetX[index], OffsetY[index]);
        }

        public static Direction2D FromOffset(int dx, int dy)
        {
            if (dx == 0 && dy > 0) return Direction2D.North;
            if (dx > 0 && dy > 0) return Direction2D.NorthEast;
            if (dx > 0 && dy == 0) return Direction2D.East;
            if (dx > 0 && dy < 0) return Direction2D.SouthEast;
            if (dx == 0 && dy < 0) return Direction2D.South;
            if (dx < 0 && dy < 0) return Direction2D.SouthWest;
            if (dx < 0 && dy == 0) return Direction2D.West;
            if (dx < 0 && dy > 0) return Direction2D.NorthWest;
            return Direction2D.None;
        }
    }
}

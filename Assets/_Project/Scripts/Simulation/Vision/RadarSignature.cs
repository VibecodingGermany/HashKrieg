namespace Nova.Simulation.Vision
{
    /// <summary>
    /// A radar minimap signature (docs/tech/FogOfWar.md section 3): a grid
    /// position hint without any entity reference. By design a ping carries
    /// no <c>EntityId</c>, no unit data and no targeting permission — Combat
    /// must not address a pinged object without <see cref="TeamView.IsVisible"/>
    /// on its cell, and hidden target ids must never leak through this API
    /// (Hidden-World contract, Testing.md section 5).
    /// </summary>
    public readonly struct RadarSignature
    {
        public int GridX { get; }
        public int GridY { get; }

        public RadarSignature(int gridX, int gridY)
        {
            GridX = gridX;
            GridY = gridY;
        }
    }
}

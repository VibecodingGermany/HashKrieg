namespace Nova.Simulation.Vision
{
    /// <summary>
    /// Represents the Fog of War vision state of a single map cell for a player.
    /// </summary>
    public enum VisionState : byte
    {
        Unexplored = 0, // Completely black, terrain and entities hidden
        Explored = 1,   // Shaded grey, terrain revealed, live entities hidden
        Visible = 2     // Bright, full line-of-sight visibility
    }
}

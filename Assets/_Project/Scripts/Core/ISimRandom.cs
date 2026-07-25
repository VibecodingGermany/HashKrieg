namespace Nova.Core
{
    /// <summary>
    /// Seedable, 100% deterministic Pseudo-Random Number Generator interface.
    /// Replaces System.Random and UnityEngine.Random in all simulation code.
    /// </summary>
    public interface ISimRandom
    {
        ulong Seed { get; }
        uint NextUInt();
        int NextInt(int minValue, int maxValue);
        float NextFloat();
        ISimRandom Clone();
    }
}

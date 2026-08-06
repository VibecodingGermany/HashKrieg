using System;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

namespace Nova.Gameplay
{
    /// <summary>
    /// Read-only classification derived from the authoritative definition
    /// table: a role is a PRODUCER role when at least one unit definition
    /// names it as its <see cref="SimUnitDefinition.ProducerRole"/> (MS-1:
    /// HQ, Refinery, Barracks, VehicleFactory — the producer assignment of
    /// D-077).
    /// <para>
    /// This restates <c>ProductionSystem.IsProducerRole</c>, which is private
    /// inside the simulation and must stay untouched; the input layer needs
    /// the same answer (a right-click on the ground with a producer building
    /// as the lead selection sets that building's rally point instead of
    /// issuing a move). The rule is rebuilt here from the same source —
    /// <see cref="SimDefinitions.AllUnits"/>, the very table the sim
    /// validates against — so it cannot drift from the executor's verdict.
    /// </para>
    /// </summary>
    public static class ProducerBuildingRoles
    {
        private static readonly bool[] ByRole = BuildTable();

        /// <summary>True when the role produces at least one unit definition (either faction).</summary>
        public static bool IsProducerRole(UnitRole role)
        {
            return ByRole[(int)role];
        }

        private static bool[] BuildTable()
        {
            // UnitRole is a byte enum; a full-domain table keeps every lookup
            // range-safe without a bounds check.
            var table = new bool[256];
            ReadOnlySpan<SimUnitDefinition> units = SimDefinitions.AllUnits;
            for (int i = 0; i < units.Length; i++)
            {
                table[(int)units[i].ProducerRole] = true;
            }
            return table;
        }
    }
}

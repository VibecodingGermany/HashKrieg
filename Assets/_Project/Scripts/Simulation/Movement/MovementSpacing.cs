using Nova.Core;
using Nova.Simulation.State;

namespace Nova.Simulation.Movement
{
    /// <summary>
    /// How much room a unit takes on the ground when units steer around each
    /// other and claim group-move slots (D-116). Separate from
    /// <see cref="UnitState.Radius"/>, which combat uses for range tests and
    /// stays 0.5 for every unit so weapon reach does not shift.
    /// <para>
    /// The values follow the art's view size (ArtViewScale, D-113): a soldier
    /// is ~0.4 cells wide, a battle tank ~1.6. With one shared 0.5 radius,
    /// tanks parked at contact distance overlapped visibly and infantry
    /// stood a full cell apart.
    /// </para>
    /// All values are exact Q16.16 integers derived from hundredths of a cell.
    /// </summary>
    public static class MovementSpacing
    {
        /// <summary>Largest spacing radius of any unit; bounds the neighbour query.</summary>
        public static readonly SimFixed MaxRadius = Hundredths(78);

        /// <summary>Units at or above this radius claim a slot plus its 8 neighbours in group moves.</summary>
        public static readonly SimFixed LargeRadius = Hundredths(60);

        private static readonly SimFixed Infantry = Hundredths(30);
        private static readonly SimFixed AntiArmor = Hundredths(32);
        private static readonly SimFixed Medium = Hundredths(50);
        private static readonly SimFixed LightTank = Hundredths(62);
        private static readonly SimFixed Harvester = Hundredths(70);
        private static readonly SimFixed Heavy = Hundredths(78);

        /// <summary>Spacing radius of a mobile role; immobile entities keep their state radius.</summary>
        public static SimFixed RadiusOf(in UnitState unit)
        {
            if (unit.MoveSpeed <= SimFixed.Zero) return unit.Radius;
            switch (unit.Role)
            {
                case UnitRole.BasicInfantry: return Infantry;
                case UnitRole.AntiArmorInfantry: return AntiArmor;
                case UnitRole.Builder: return Medium;
                case UnitRole.ScoutVehicle: return Medium;
                case UnitRole.LightTank: return LightTank;
                case UnitRole.Harvester: return Harvester;
                case UnitRole.BattleTank: return Heavy;
                case UnitRole.Artillery: return Heavy;
                default: return unit.Radius;
            }
        }

        /// <summary>True when a unit needs a free ring of cells around its group-move slot.</summary>
        public static bool IsLarge(in UnitState unit) => RadiusOf(in unit) >= LargeRadius;

        private static SimFixed Hundredths(int h) => SimFixed.FromRaw((SimFixed.OneRaw * h + 50) / 100);
    }
}

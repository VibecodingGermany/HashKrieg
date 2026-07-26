using Nova.Core;
using Nova.Simulation.Combat;
using Nova.Simulation.State;

namespace Nova.Simulation.Definitions
{
    /// <summary>
    /// Canonical numeric definition of one MS-1 building role
    /// (quality/content/mvp-v1.json section 3). Pure value type, engine-free.
    /// </summary>
    public readonly struct SimBuildingDefinition
    {
        /// <summary>Stable numeric definition id carried by PlaceBuilding payloads (nonzero).</summary>
        public ushort DefinitionId { get; }

        /// <summary>The entity role a completed building of this definition carries.</summary>
        public UnitRole Role { get; }

        /// <summary>Aetherium cost in AE, charged in full at placement.</summary>
        public int CostAE { get; }

        /// <summary>Construction duration in ticks at full power.</summary>
        public int BuildTicks { get; }

        /// <summary>Power this building feeds into its owner's grid once completed.</summary>
        public int PowerProvided { get; }

        /// <summary>Power this building draws from its owner's grid once completed.</summary>
        public int PowerRequired { get; }

        /// <summary>True when placement requires a completed own building of <see cref="PrerequisiteRole"/>.</summary>
        public bool HasPrerequisite { get; }

        /// <summary>Required completed own building role; meaningful only when <see cref="HasPrerequisite"/>.</summary>
        public UnitRole PrerequisiteRole { get; }

        /// <summary>Hit points of the completed building.</summary>
        public int MaxHealth { get; }

        /// <summary>
        /// Armor class this building presents to incoming fire
        /// (docs/gamedesign/ArmorSystem.md: every building type is
        /// <see cref="Combat.ArmorClass.Building"/>). Column axis of
        /// <see cref="Combat.DamageMatrix"/>.
        /// </summary>
        public ArmorClass ArmorClass { get; }

        /// <summary>
        /// Damage type of this building's weapon; meaningless while
        /// <see cref="AttackDamage"/> is 0 (unarmed buildings carry
        /// <see cref="Combat.DamageType.Kinetic"/> as a neutral placeholder
        /// that is never applied).
        /// </summary>
        public DamageType DamageType { get; }

        /// <summary>
        /// Base damage per shot BEFORE the armor multiplier
        /// (docs/gamedesign/Weapons.md, führend per D-047).
        /// <b>0 means unarmed and is the single unambiguous marker for it:</b>
        /// an entity with 0 base damage never fires and can never reduce a
        /// target's health. Only the DefensePlatform is armed in MS-1 —
        /// buildings CAN shoot, the other eight simply do not.
        /// </summary>
        public int AttackDamage { get; }

        /// <summary>
        /// Weapon range in tiles; 1 tile == 1 m (D-034/D-047), so the meter
        /// value is numerically identical. 0 for unarmed buildings.
        /// </summary>
        public int AttackRangeTiles { get; }

        /// <summary>
        /// Firing interval in whole simulation ticks, converted from the
        /// Weapons.md seconds value at the canonical 10 Hz tick rate. 0 for
        /// unarmed buildings.
        /// </summary>
        public int AttackCooldownTicks { get; }

        public SimBuildingDefinition(
            ushort definitionId, UnitRole role, int costAE, int buildTicks,
            int powerProvided, int powerRequired,
            bool hasPrerequisite, UnitRole prerequisiteRole, int maxHealth,
            ArmorClass armorClass, DamageType damageType,
            int attackDamage, int attackRangeTiles, int attackCooldownTicks)
        {
            DefinitionId = definitionId;
            Role = role;
            CostAE = costAE;
            BuildTicks = buildTicks;
            PowerProvided = powerProvided;
            PowerRequired = powerRequired;
            HasPrerequisite = hasPrerequisite;
            PrerequisiteRole = prerequisiteRole;
            MaxHealth = maxHealth;
            ArmorClass = armorClass;
            DamageType = damageType;
            AttackDamage = attackDamage;
            AttackRangeTiles = attackRangeTiles;
            AttackCooldownTicks = attackCooldownTicks;
        }
    }

    /// <summary>
    /// Canonical numeric definition of one MS-1 unit role
    /// (quality/content/mvp-v1.json section 4). Pure value type, engine-free.
    /// </summary>
    public readonly struct SimUnitDefinition
    {
        /// <summary>Stable numeric definition id carried by QueueUnit payloads (nonzero).</summary>
        public ushort DefinitionId { get; }

        /// <summary>The entity role a produced unit of this definition carries.</summary>
        public UnitRole Role { get; }

        /// <summary>Aetherium cost in AE per unit, charged at enqueue.</summary>
        public int CostAE { get; }

        /// <summary>Production duration in ticks per unit at full power.</summary>
        public int BuildTicks { get; }

        /// <summary>Technology tier (1 or 2; tier 3 is not MS-1 content). Tier 2 requires the owner's T2 unlock.</summary>
        public byte Tier { get; }

        /// <summary>Role of the finished building that produces this unit.</summary>
        public UnitRole ProducerRole { get; }

        /// <summary>Hit points of the produced unit.</summary>
        public int MaxHealth { get; }

        /// <summary>Movement speed of the produced unit (m/tick-domain fixed point).</summary>
        public SimFixed MoveSpeed { get; }

        /// <summary>
        /// Armor class this unit presents to incoming fire
        /// (docs/gamedesign/ArmorSystem.md). Column axis of
        /// <see cref="Combat.DamageMatrix"/>. Note that ArmorSystem.md places
        /// BOTH the Light Tank and the Battle Tank in
        /// <see cref="Combat.ArmorClass.Medium"/> and reserves
        /// <see cref="Combat.ArmorClass.Heavy"/> for the (non-MS-1) Heavy
        /// Tank — followed faithfully here.
        /// </summary>
        public ArmorClass ArmorClass { get; }

        /// <summary>
        /// Damage type of this unit's weapon; meaningless while
        /// <see cref="AttackDamage"/> is 0 (unarmed units carry
        /// <see cref="Combat.DamageType.Kinetic"/> as a neutral placeholder
        /// that is never applied).
        /// </summary>
        public DamageType DamageType { get; }

        /// <summary>
        /// Base damage per shot BEFORE the armor multiplier
        /// (docs/gamedesign/Weapons.md, führend per D-047).
        /// <b>0 means unarmed and is the single unambiguous marker for it:</b>
        /// an entity with 0 base damage never fires and can never reduce a
        /// target's health. Builder and Harvester are the MS-1 unarmed units.
        /// </summary>
        public int AttackDamage { get; }

        /// <summary>
        /// Weapon range in tiles; 1 tile == 1 m (D-034/D-047), so the meter
        /// value is numerically identical. 0 for unarmed units.
        /// </summary>
        public int AttackRangeTiles { get; }

        /// <summary>
        /// Firing interval in whole simulation ticks, converted from the
        /// Weapons.md seconds value at the canonical 10 Hz tick rate. 0 for
        /// unarmed units.
        /// </summary>
        public int AttackCooldownTicks { get; }

        public SimUnitDefinition(
            ushort definitionId, UnitRole role, int costAE, int buildTicks,
            byte tier, UnitRole producerRole, int maxHealth, SimFixed moveSpeed,
            ArmorClass armorClass, DamageType damageType,
            int attackDamage, int attackRangeTiles, int attackCooldownTicks)
        {
            DefinitionId = definitionId;
            Role = role;
            CostAE = costAE;
            BuildTicks = buildTicks;
            Tier = tier;
            ProducerRole = producerRole;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            ArmorClass = armorClass;
            DamageType = damageType;
            AttackDamage = attackDamage;
            AttackRangeTiles = attackRangeTiles;
            AttackCooldownTicks = attackCooldownTicks;
        }
    }

    /// <summary>
    /// Canonical, engine-free numeric definition table of the MS-1 production
    /// and construction slice: the nine building roles and eight unit roles
    /// of quality/content/mvp-v1.json with their costs, build times, power
    /// figures and prerequisites. Static data only — no ScriptableObjects,
    /// because the simulation core carries zero engine dependencies.
    /// <para>
    /// ALL values are documented Q-040 provisionals, not ratified balancing:
    /// they are plausible placeholders chosen so the MS-1 start (1.000 AE,
    /// HQ + completed Refinery) works and the tech chain is exercisable, and
    /// they are replaced once GameDatabase/Definitions are wired
    /// (docs/gamedesign/Buildings.md, Infantry.md and Vehicles.md stay the
    /// leading sources for final values). Definition ids are stable wire
    /// identifiers (Commands.md schema v1): buildings 1–9 and units 1–8 in
    /// manifest order; raw id 0 is invalid.
    /// </para>
    /// <para>
    /// Producer assignment (documented provisional, Q-040): the HQ produces
    /// Builder and Harvester, the Barracks produces both infantry roles, the
    /// VehicleFactory produces all four vehicle roles. Tier-2 units
    /// (AntiArmorInfantry, BattleTank, Artillery) additionally require the
    /// owner's T2 unlock (a completed ResearchLab — mvp-v1.json
    /// technology.researchLabCompletionUnlocksTier2; no research upgrades,
    /// no research queue, no tier 3).
    /// </para>
    /// <para>
    /// COMBAT VALUES (ArmorClass / DamageType / AttackDamage /
    /// AttackRangeTiles / AttackCooldownTicks) are NOT Q-040 provisionals like
    /// the economy figures above — they are the ratified content values and
    /// this table is where they live. Weapon numbers come from
    /// docs/gamedesign/Weapons.md (führend per D-047), armor classes and the
    /// counter multipliers from docs/gamedesign/ArmorSystem.md (authoritative
    /// over the drifted 6x4 and 5x4 summaries in Infantry.md and Vehicles.md;
    /// see <see cref="Combat.DamageMatrix"/> for the full grounds). Cooldowns
    /// are the Weapons.md seconds converted at the canonical 10 Hz tick rate,
    /// ranges are tiles with 1 tile == 1 m (D-034/D-047).
    /// <br/>
    /// Unarmed is expressed ONLY as <c>AttackDamage: 0</c> — there is no
    /// separate flag to fall out of sync with. Builder, Harvester and the
    /// eight non-defensive buildings are unarmed; the DefensePlatform is armed
    /// because buildings CAN shoot.
    /// </para>
    /// </summary>
    public static class SimDefinitions
    {
        /// <summary>Provisional building footprint in cells: every MS-1 building occupies 3x3 (Q-040 candidate).</summary>
        public const int BuildingFootprintCells = 3;

        /// <summary>
        /// Neutral damage-type placeholder for unarmed entities. Never
        /// applied: <see cref="Combat.DamageMatrix.Resolve"/> short-circuits
        /// on <c>AttackDamage == 0</c>, so the type is inert by construction.
        /// Named rather than inlined so "unarmed" reads as a decision.
        /// </summary>
        private const DamageType Unarmed = DamageType.Kinetic;

        private static readonly SimBuildingDefinition[] Buildings =
        {
            new SimBuildingDefinition(1, UnitRole.HQ,              costAE: 2000, buildTicks: 600, powerProvided: 30,  powerRequired: 0,  hasPrerequisite: false, prerequisiteRole: UnitRole.Unit,      maxHealth: 2000, armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(2, UnitRole.Power,           costAE: 300,  buildTicks: 150, powerProvided: 100, powerRequired: 0,  hasPrerequisite: false, prerequisiteRole: UnitRole.Unit,      maxHealth: 400,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(3, UnitRole.Refinery,        costAE: 600,  buildTicks: 300, powerProvided: 0,   powerRequired: 20, hasPrerequisite: true,  prerequisiteRole: UnitRole.Power,     maxHealth: 800,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(4, UnitRole.Storage,         costAE: 200,  buildTicks: 100, powerProvided: 0,   powerRequired: 10, hasPrerequisite: false, prerequisiteRole: UnitRole.Unit,      maxHealth: 400,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(5, UnitRole.Barracks,        costAE: 500,  buildTicks: 250, powerProvided: 0,   powerRequired: 10, hasPrerequisite: false, prerequisiteRole: UnitRole.Unit,      maxHealth: 600,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(6, UnitRole.VehicleFactory,  costAE: 1200, buildTicks: 400, powerProvided: 0,   powerRequired: 20, hasPrerequisite: true,  prerequisiteRole: UnitRole.Refinery,  maxHealth: 900,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(7, UnitRole.ResearchLab,     costAE: 1500, buildTicks: 450, powerProvided: 0,   powerRequired: 30, hasPrerequisite: true,  prerequisiteRole: UnitRole.Barracks,  maxHealth: 700,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(8, UnitRole.Radar,           costAE: 800,  buildTicks: 200, powerProvided: 0,   powerRequired: 15, hasPrerequisite: true,  prerequisiteRole: UnitRole.Power,     maxHealth: 500,  armorClass: ArmorClass.Building, damageType: Unarmed,             attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(9, UnitRole.DefensePlatform, costAE: 400,  buildTicks: 150, powerProvided: 0,   powerRequired: 10, hasPrerequisite: true,  prerequisiteRole: UnitRole.Power,     maxHealth: 600,  armorClass: ArmorClass.Building, damageType: DamageType.Kinetic,  attackDamage: 20, attackRangeTiles: 10, attackCooldownTicks: 10),
        };

        private static readonly SimUnitDefinition[] Units =
        {
            new SimUnitDefinition(1, UnitRole.Builder,            costAE: 100,  buildTicks: 150, tier: 1, producerRole: UnitRole.HQ,             maxHealth: 100, moveSpeed: SimFixed.FromInt(3),      armorClass: ArmorClass.Light,    damageType: Unarmed,               attackDamage: 0,   attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimUnitDefinition(2, UnitRole.Harvester,          costAE: 700,  buildTicks: 300, tier: 1, producerRole: UnitRole.HQ,             maxHealth: 300, moveSpeed: SimFixed.FromRaw(163840), armorClass: ArmorClass.Light,    damageType: Unarmed,               attackDamage: 0,   attackRangeTiles: 0,  attackCooldownTicks: 0), // 2.5
            new SimUnitDefinition(3, UnitRole.BasicInfantry,      costAE: 100,  buildTicks: 100, tier: 1, producerRole: UnitRole.Barracks,       maxHealth: 100, moveSpeed: SimFixed.FromInt(4),      armorClass: ArmorClass.Infantry, damageType: DamageType.Kinetic,    attackDamage: 10,  attackRangeTiles: 7,  attackCooldownTicks: 9),
            new SimUnitDefinition(4, UnitRole.AntiArmorInfantry,  costAE: 300,  buildTicks: 150, tier: 2, producerRole: UnitRole.Barracks,       maxHealth: 120, moveSpeed: SimFixed.FromRaw(229376), armorClass: ArmorClass.Infantry, damageType: DamageType.Explosive,  attackDamage: 50,  attackRangeTiles: 10, attackCooldownTicks: 25), // 3.5
            new SimUnitDefinition(5, UnitRole.ScoutVehicle,       costAE: 400,  buildTicks: 200, tier: 1, producerRole: UnitRole.VehicleFactory, maxHealth: 150, moveSpeed: SimFixed.FromInt(6),      armorClass: ArmorClass.Light,    damageType: DamageType.Kinetic,    attackDamage: 12,  attackRangeTiles: 8,  attackCooldownTicks: 10),
            new SimUnitDefinition(6, UnitRole.LightTank,          costAE: 700,  buildTicks: 300, tier: 1, producerRole: UnitRole.VehicleFactory, maxHealth: 300, moveSpeed: SimFixed.FromInt(4),      armorClass: ArmorClass.Medium,   damageType: DamageType.Kinetic,    attackDamage: 35,  attackRangeTiles: 9,  attackCooldownTicks: 20),
            // Owner ruling (see D-074 consequences): BattleTank is Heavy, not the
            // Medium that ArmorSystem.md assigns it. ArmorSystem.md reserves Heavy
            // for the Heavy Tank, which MS-1 does not ship, which would have left
            // the Heavy column unexercised and the "Kinetic 0.25 vs Heavy forces
            // rockets" counter unreachable. Promoting the BattleTank is the
            // smallest change that makes that counter play in MS-1.
            new SimUnitDefinition(7, UnitRole.BattleTank,         costAE: 1200, buildTicks: 400, tier: 2, producerRole: UnitRole.VehicleFactory, maxHealth: 500, moveSpeed: SimFixed.FromInt(3),      armorClass: ArmorClass.Heavy,    damageType: DamageType.Kinetic,    attackDamage: 60,  attackRangeTiles: 10, attackCooldownTicks: 25),
            new SimUnitDefinition(8, UnitRole.Artillery,          costAE: 1500, buildTicks: 500, tier: 2, producerRole: UnitRole.VehicleFactory, maxHealth: 200, moveSpeed: SimFixed.FromRaw(163840), armorClass: ArmorClass.Light,    damageType: DamageType.Explosive,  attackDamage: 110, attackRangeTiles: 20, attackCooldownTicks: 70), // 2.5
        };

        /// <summary>Number of building definitions (ids 1..<see cref="BuildingCount"/>).</summary>
        public static int BuildingCount => Buildings.Length;

        /// <summary>Number of unit definitions (ids 1..<see cref="UnitCount"/>).</summary>
        public static int UnitCount => Units.Length;

        /// <summary>Looks up a building definition by its stable numeric id.</summary>
        public static bool TryGetBuilding(ushort definitionId, out SimBuildingDefinition definition)
        {
            if (definitionId >= 1 && definitionId <= Buildings.Length)
            {
                definition = Buildings[definitionId - 1];
                return true;
            }
            definition = default;
            return false;
        }

        /// <summary>Looks up the building definition of a building role.</summary>
        public static bool TryGetBuilding(UnitRole role, out SimBuildingDefinition definition)
        {
            for (int i = 0; i < Buildings.Length; i++)
            {
                if (Buildings[i].Role == role)
                {
                    definition = Buildings[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }

        /// <summary>Looks up a unit definition by its stable numeric id.</summary>
        public static bool TryGetUnit(ushort definitionId, out SimUnitDefinition definition)
        {
            if (definitionId >= 1 && definitionId <= Units.Length)
            {
                definition = Units[definitionId - 1];
                return true;
            }
            definition = default;
            return false;
        }

        /// <summary>
        /// Looks up the unit definition of a unit role (mirror of
        /// <see cref="TryGetBuilding(UnitRole, out SimBuildingDefinition)"/>).
        /// Roles map one-to-one onto definitions in MS-1, so this is the
        /// role-side entry point the combat weapon table is built from.
        /// </summary>
        public static bool TryGetUnit(UnitRole role, out SimUnitDefinition definition)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                if (Units[i].Role == role)
                {
                    definition = Units[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }

        /// <summary>True when the role is one of the nine MS-1 building roles.</summary>
        public static bool IsBuildingRole(UnitRole role)
        {
            return role >= UnitRole.HQ && role <= UnitRole.DefensePlatform;
        }
    }
}

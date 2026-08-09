using System;
using Nova.Core;
using Nova.Simulation.Combat;
using Nova.Simulation.State;

namespace Nova.Simulation.Definitions
{
    /// <summary>
    /// Set of completed own building roles required before placement. Bit n
    /// is the stable wire value n of <see cref="UnitRole"/>; UnitRole itself
    /// remains an unchanged single-value wire enum.
    /// </summary>
    [Flags]
    public enum UnitRoleMask : uint
    {
        None = 0,
        HQ = 1u << (int)UnitRole.HQ,
        Refinery = 1u << (int)UnitRole.Refinery,
        Power = 1u << (int)UnitRole.Power,
        Storage = 1u << (int)UnitRole.Storage,
        Barracks = 1u << (int)UnitRole.Barracks,
        VehicleFactory = 1u << (int)UnitRole.VehicleFactory,
        ResearchLab = 1u << (int)UnitRole.ResearchLab,
        Radar = 1u << (int)UnitRole.Radar,
        DefensePlatform = 1u << (int)UnitRole.DefensePlatform,
    }

    /// <summary>
    /// Canonical numeric definition of one MS-1 building role of ONE faction
    /// (quality/content/mvp-v1.json section 3, factions[0]/factions[1]).
    /// Pure value type, engine-free.
    /// </summary>
    public readonly struct SimBuildingDefinition
    {
        /// <summary>Stable numeric definition id carried by PlaceBuilding payloads (nonzero; see <see cref="SimDefinitions"/> for the id rule).</summary>
        public ushort DefinitionId { get; }

        /// <summary>The faction this definition belongs to (id axis: Alliance = role value, Legion = role value + 17).</summary>
        public FactionId Faction { get; }

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

        /// <summary>All completed own building roles required for placement (all-of semantics).</summary>
        public UnitRoleMask PrerequisiteRoles { get; }

        /// <summary>True when <see cref="PrerequisiteRoles"/> is non-empty.</summary>
        public bool HasPrerequisite => PrerequisiteRoles != UnitRoleMask.None;

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
            ushort definitionId, FactionId faction, UnitRole role, int costAE, int buildTicks,
            int powerProvided, int powerRequired,
            UnitRoleMask prerequisiteRoles, int maxHealth,
            ArmorClass armorClass, DamageType damageType,
            int attackDamage, int attackRangeTiles, int attackCooldownTicks)
        {
            DefinitionId = definitionId;
            Faction = faction;
            Role = role;
            CostAE = costAE;
            BuildTicks = buildTicks;
            PowerProvided = powerProvided;
            PowerRequired = powerRequired;
            PrerequisiteRoles = prerequisiteRoles;
            MaxHealth = maxHealth;
            ArmorClass = armorClass;
            DamageType = damageType;
            AttackDamage = attackDamage;
            AttackRangeTiles = attackRangeTiles;
            AttackCooldownTicks = attackCooldownTicks;
        }
    }

    /// <summary>
    /// Canonical numeric definition of one MS-1 unit role of ONE faction
    /// (quality/content/mvp-v1.json section 4, factions[0]/factions[1]).
    /// Pure value type, engine-free.
    /// </summary>
    public readonly struct SimUnitDefinition
    {
        /// <summary>Stable numeric definition id carried by QueueUnit payloads (nonzero; see <see cref="SimDefinitions"/> for the id rule).</summary>
        public ushort DefinitionId { get; }

        /// <summary>The faction this definition belongs to (id axis: Alliance = role value, Legion = role value + 17).</summary>
        public FactionId Faction { get; }

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
        /// Tank — followed faithfully here for the Alliance column; the
        /// Legion BattleTank inherits the same D-074 promotion.
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

        /// <summary>
        /// Aetherium cargo capacity in AE. Meaningful ONLY on the Harvester
        /// row — the faction identity split of quality/content/mvp-v1.json
        /// (factions[0].identity.harvesterCargoAE 330,
        /// factions[1].identity.harvesterCargoAE 300; the manifest is the
        /// byte-immutable authority, the values are restated from the GDD
        /// prose). Every other unit role carries 0: no cargo, no capacity.
        /// </summary>
        public int CargoCapacityAE { get; }

        public SimUnitDefinition(
            ushort definitionId, FactionId faction, UnitRole role, int costAE, int buildTicks,
            byte tier, UnitRole producerRole, int maxHealth, SimFixed moveSpeed,
            ArmorClass armorClass, DamageType damageType,
            int attackDamage, int attackRangeTiles, int attackCooldownTicks,
            int cargoCapacityAE = 0)
        {
            DefinitionId = definitionId;
            Faction = faction;
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
            CargoCapacityAE = cargoCapacityAE;
        }
    }

    /// <summary>
    /// Canonical, engine-free numeric definition table of the MS-1 production
    /// and construction slice WITH the faction axis: 34 definitions — the
    /// nine building roles and eight unit roles of quality/content/mvp-v1.json
    /// once per faction (factions[0] Alliance, factions[1] Legion). Static
    /// data only — no ScriptableObjects, because the simulation core carries
    /// zero engine dependencies.
    /// <para>
    /// DEFINITION ID RULE (stable wire identifiers, Commands.md schema v1):
    /// the ALLIANCE id of a definition IS the wire value of its
    /// <see cref="UnitRole"/> (1..17 — the enum deliberately numbers the
    /// seventeen content roles 1..17); the LEGION id adds
    /// <see cref="FactionDefinitionOffset"/> (18..34). Raw id 0 is invalid.
    /// Every id is globally unique across buildings and units, so a payload
    /// id resolves to exactly one definition row, and the row's
    /// <see cref="SimBuildingDefinition.Faction"/> tells which faction may
    /// build it. The pre-faction ids (buildings 1–9 and units 1–8 in manifest
    /// order, overlapping) are retired with the pre-G1 format reset — no
    /// evidence binds them.
    /// </para>
    /// <para>
    /// VALUE SOURCES (führend in this order): the concrete per-faction
    /// numbers of docs/gamedesign/Buildings.md (building costs, build times,
    /// power — review finding F-03/D-047 makes it the leading building
    /// source), Vehicles.md and Infantry.md (unit costs, build times, HP,
    /// ranges), and docs/gamedesign/Weapons.md (weapon damage, range,
    /// cadence — führend per D-047, seconds converted at the canonical 10 Hz
    /// tick rate). Armor classes and the counter multipliers come from
    /// docs/gamedesign/ArmorSystem.md (authoritative per D-074).
    /// </para>
    /// <para>
    /// DERIVATION RULE where the GDDs name no concrete per-faction number
    /// (building HP — Buildings.md gives only armor-class bands, not hit
    /// points — and the Legion Scout's per-shot damage): integer-percent
    /// derivation from the Alliance row, <c>(alliance * percent) / 100</c>,
    /// with <see cref="LegionHealthPercent"/> for hit points and
    /// <see cref="LegionDamagePercent"/> for base damage. The concrete value
    /// beats the derivation wherever a GDD names one (D-075): Vehicles.md
    /// carries a concrete Legion damage line for the three combat vehicles —
    /// Räuber 28, Koloss 50, Donnerkanone 60 — so those rows are authored
    /// verbatim and NOT derived (the derivation would have produced 29/51/93).
    /// Where Weapons.md
    /// publishes a band for BOTH factions (Panzerabwehrrakete/Raketenwerfer
    /// 40–60), the Alliance keeps the ratified table value and the Legion
    /// takes the band minimum — the documented pick, not an invented number.
    /// Legion weapon cooldowns equal the Alliance cadence: no GDD source
    /// differentiates firing cadence, and the Legion salvo/splash identity
    /// (factions[1].identity.weaponProfile) is registered ScopeLedger debt,
    /// not this slice. The DefensePlatform weapon is identical for both
    /// factions because the platform modules are faction-neutral content
    /// (Buildings.md section 3).
    /// </para>
    /// <para>
    /// DELIBERATELY STILL FLAT (documented provisional, NOT faction
    /// content): <see cref="SimUnitDefinition.MoveSpeed"/> — the GDD speeds
    /// are authored in m/s while the simulation runs the m/tick domain, the
    /// conversion is unratified, and faction identity (Factions.md) does not
    /// differentiate speed. Both factions carry the existing provisional
    /// values so the canonical opening position keeps its move speeds.
    /// Harvester cargo capacity is NOT flat: it is faction content
    /// (<see cref="SimUnitDefinition.CargoCapacityAE"/>, Allianz 330 / Legion
    /// 300 — quality/content/mvp-v1.json factions[i].identity.harvesterCargoAE).
    /// </para>
    /// <para>
    /// Producer assignment (D-077, identical for both factions): the HQ
    /// produces the Builder, the REFINERY produces the Harvester (the
    /// classic loop start — before D-077 the HQ produced both), the
    /// Barracks produces both infantry roles, the VehicleFactory produces
    /// all four vehicle roles. The same decision removed the Refinery's
    /// Power-plant prerequisite in both factions; its power DRAW (Alliance
    /// 20, Legion 15) is unchanged, so a Power plant is still needed once
    /// Refinery + Barracks exceed the HQ's 30. Tier-2 units (AntiArmorInfantry,
    /// BattleTank, Artillery) additionally require the owner's T2 unlock (a
    /// completed ResearchLab — mvp-v1.json
    /// technology.researchLabCompletionUnlocksTier2; no research
    /// upgrades, no research queue, no tier 3).
    /// </para>
    /// <para>
    /// Unarmed is expressed ONLY as <c>AttackDamage: 0</c> — there is no
    /// separate flag to fall out of sync with. Builder, Harvester and the
    /// eight non-defensive buildings are unarmed in BOTH factions; the
    /// DefensePlatform is armed because buildings CAN shoot.
    /// </para>
    /// </summary>
    public static class SimDefinitions
    {
        /// <summary>Provisional building footprint in cells: every MS-1 building occupies 3x3 (Q-040 candidate).</summary>
        public const int BuildingFootprintCells = 3;

        /// <summary>Content roles per faction: nine building roles plus eight unit roles.</summary>
        public const int RolesPerFaction = 17;

        /// <summary>Legion definition ids are the Alliance (role-value) id plus this offset — the documented id rule.</summary>
        public const int FactionDefinitionOffset = RolesPerFaction;

        /// <summary>Highest valid definition id (Legion Artillery, 17 + 17). Raw id 0 is invalid.</summary>
        public const int MaxDefinitionId = 2 * RolesPerFaction;

        /// <summary>Building definitions per faction (the nine MS-1 building roles).</summary>
        public const int BuildingsPerFaction = 9;

        /// <summary>Unit definitions per faction (the eight MS-1 unit roles).</summary>
        public const int UnitsPerFaction = 8;

        /// <summary>Total definitions over both factions (34).</summary>
        public const int TotalDefinitionCount = 2 * RolesPerFaction;

        /// <summary>Legion hit points as integer percent of the Alliance row where the GDDs name no concrete building/unit HP (derivation rule, see class remarks).</summary>
        public const int LegionHealthPercent = 85;

        /// <summary>Legion base damage as integer percent of the Alliance row where Weapons.md lists no Legion weapon line (derivation rule, see class remarks).</summary>
        public const int LegionDamagePercent = 85;

        /// <summary>
        /// Largest harvester cargo capacity over both factions (the Alliance
        /// 330; the Legion carries 300). This is the format-level HARD CAP
        /// the entity store's snapshot parser validates cargo against: an
        /// entity block can hold harvesters of both factions, so the block
        /// itself cannot judge the per-faction bound — it rejects only what
        /// no faction could ever carry.
        /// </summary>
        public const int MaxHarvesterCargoCapacityAE = 330;

        /// <summary>
        /// The harvester cargo capacity of one faction, resolved from its
        /// canonical Harvester definition row (Allianz 330, Legion 300) —
        /// the single source the economy clamps gathering against.
        /// </summary>
        public static int HarvesterCargoCapacityAE(FactionId faction)
        {
            if (TryGetUnit(faction, UnitRole.Harvester, out SimUnitDefinition harvester))
            {
                return harvester.CargoCapacityAE;
            }
            throw new ArgumentOutOfRangeException(nameof(faction), faction, "unknown faction");
        }

        /// <summary>
        /// Neutral damage-type placeholder for unarmed entities. Never
        /// applied: <see cref="Combat.DamageMatrix.Resolve"/> short-circuits
        /// on <c>AttackDamage == 0</c>, so the type is inert by construction.
        /// Named rather than inlined so "unarmed" reads as a decision.
        /// </summary>
        private const DamageType Unarmed = DamageType.Kinetic;

        // ------------------------------------------------------------------
        // Alliance rows — factions[0]. Building cost/build-time/power:
        // Buildings.md section 2 (führend); building HP: existing provisional
        // (Buildings.md names armor-class bands, no hit points). Unit
        // cost/build-time/HP: Vehicles.md and Infantry.md tables; infantry
        // build times stay provisional (Infantry.md names only the T1/T2
        // frames, no per-unit values). Weapon damage/range/cadence:
        // Weapons.md (führend per D-047), unchanged ratified values.
        // ------------------------------------------------------------------
        private static readonly SimBuildingDefinition[] Buildings =
        {
            // --- Alliance (factions[0]) ---
            new SimBuildingDefinition(3,  FactionId.Alliance, UnitRole.HQ,              costAE: 2500, buildTicks: 600, powerProvided: 30,  powerRequired: 0,  prerequisiteRoles: UnitRoleMask.None,                         maxHealth: 2000, armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(5,  FactionId.Alliance, UnitRole.Power,           costAE: 450,  buildTicks: 150, powerProvided: 100, powerRequired: 0,  prerequisiteRoles: UnitRoleMask.HQ,                           maxHealth: 400,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(4,  FactionId.Alliance, UnitRole.Refinery,        costAE: 700,  buildTicks: 200, powerProvided: 0,   powerRequired: 20, prerequisiteRoles: UnitRoleMask.None,                         maxHealth: 800,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0), // D-077: no Power-plant prerequisite
            new SimBuildingDefinition(6,  FactionId.Alliance, UnitRole.Storage,         costAE: 300,  buildTicks: 100, powerProvided: 0,   powerRequired: 5,  prerequisiteRoles: UnitRoleMask.Refinery,                     maxHealth: 400,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(7,  FactionId.Alliance, UnitRole.Barracks,        costAE: 500,  buildTicks: 180, powerProvided: 0,   powerRequired: 15, prerequisiteRoles: UnitRoleMask.HQ | UnitRoleMask.Power,       maxHealth: 600,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(8,  FactionId.Alliance, UnitRole.VehicleFactory,  costAE: 900,  buildTicks: 250, powerProvided: 0,   powerRequired: 25, prerequisiteRoles: UnitRoleMask.Refinery | UnitRoleMask.Barracks, maxHealth: 900, armorClass: ArmorClass.Building, damageType: Unarmed,          attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(9,  FactionId.Alliance, UnitRole.ResearchLab,     costAE: 1000, buildTicks: 300, powerProvided: 0,   powerRequired: 30, prerequisiteRoles: UnitRoleMask.VehicleFactory,               maxHealth: 700,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(10, FactionId.Alliance, UnitRole.Radar,           costAE: 400,  buildTicks: 150, powerProvided: 0,   powerRequired: 20, prerequisiteRoles: UnitRoleMask.Power | UnitRoleMask.Barracks, maxHealth: 500, armorClass: ArmorClass.Building, damageType: Unarmed,          attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(11, FactionId.Alliance, UnitRole.DefensePlatform, costAE: 400,  buildTicks: 120, powerProvided: 0,   powerRequired: 10, prerequisiteRoles: UnitRoleMask.Power,                        maxHealth: 600,  armorClass: ArmorClass.Building, damageType: DamageType.Kinetic, attackDamage: 20, attackRangeTiles: 10, attackCooldownTicks: 10),

            // --- Legion (factions[1]): Buildings.md section 2 concrete values;
            // HP derived: (alliance * 85) / 100 (derivation rule). The
            // DefensePlatform weapon stays identical — faction-neutral module
            // content (Buildings.md section 3). ---
            new SimBuildingDefinition(20, FactionId.Legion,   UnitRole.HQ,              costAE: 2000, buildTicks: 500, powerProvided: 30,  powerRequired: 0,  prerequisiteRoles: UnitRoleMask.None,                         maxHealth: 1700, armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(22, FactionId.Legion,   UnitRole.Power,           costAE: 350,  buildTicks: 120, powerProvided: 80,  powerRequired: 0,  prerequisiteRoles: UnitRoleMask.HQ,                           maxHealth: 340,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(21, FactionId.Legion,   UnitRole.Refinery,        costAE: 550,  buildTicks: 160, powerProvided: 0,   powerRequired: 15, prerequisiteRoles: UnitRoleMask.None,                         maxHealth: 680,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0), // D-077: no Power-plant prerequisite
            new SimBuildingDefinition(23, FactionId.Legion,   UnitRole.Storage,         costAE: 250,  buildTicks: 80,  powerProvided: 0,   powerRequired: 5,  prerequisiteRoles: UnitRoleMask.Refinery,                     maxHealth: 340,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(24, FactionId.Legion,   UnitRole.Barracks,        costAE: 400,  buildTicks: 140, powerProvided: 0,   powerRequired: 10, prerequisiteRoles: UnitRoleMask.HQ | UnitRoleMask.Power,       maxHealth: 510,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(25, FactionId.Legion,   UnitRole.VehicleFactory,  costAE: 700,  buildTicks: 200, powerProvided: 0,   powerRequired: 20, prerequisiteRoles: UnitRoleMask.Refinery | UnitRoleMask.Barracks, maxHealth: 765, armorClass: ArmorClass.Building, damageType: Unarmed,          attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(26, FactionId.Legion,   UnitRole.ResearchLab,     costAE: 800,  buildTicks: 240, powerProvided: 0,   powerRequired: 25, prerequisiteRoles: UnitRoleMask.VehicleFactory,               maxHealth: 595,  armorClass: ArmorClass.Building, damageType: Unarmed,            attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(27, FactionId.Legion,   UnitRole.Radar,           costAE: 300,  buildTicks: 120, powerProvided: 0,   powerRequired: 15, prerequisiteRoles: UnitRoleMask.Power | UnitRoleMask.Barracks, maxHealth: 425, armorClass: ArmorClass.Building, damageType: Unarmed,          attackDamage: 0,  attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimBuildingDefinition(28, FactionId.Legion,   UnitRole.DefensePlatform, costAE: 300,  buildTicks: 100, powerProvided: 0,   powerRequired: 8,  prerequisiteRoles: UnitRoleMask.Power,                        maxHealth: 510,  armorClass: ArmorClass.Building, damageType: DamageType.Kinetic, attackDamage: 20, attackRangeTiles: 10, attackCooldownTicks: 10),
        };

        private static readonly SimUnitDefinition[] Units =
        {
            // --- Alliance (factions[0]) ---
            new SimUnitDefinition(1,  FactionId.Alliance, UnitRole.Builder,            costAE: 800,  buildTicks: 300, tier: 1, producerRole: UnitRole.HQ,             maxHealth: 350,  moveSpeed: SimFixed.FromInt(3),      armorClass: ArmorClass.Light,    damageType: Unarmed,               attackDamage: 0,   attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimUnitDefinition(2,  FactionId.Alliance, UnitRole.Harvester,          costAE: 700,  buildTicks: 250, tier: 1, producerRole: UnitRole.Refinery,       maxHealth: 800,  moveSpeed: SimFixed.FromRaw(163840), armorClass: ArmorClass.Light,    damageType: Unarmed,               attackDamage: 0,   attackRangeTiles: 0,  attackCooldownTicks: 0, cargoCapacityAE: 330), // 2.5; D-077: produced by the Refinery
            new SimUnitDefinition(12, FactionId.Alliance, UnitRole.BasicInfantry,      costAE: 120,  buildTicks: 100, tier: 1, producerRole: UnitRole.Barracks,       maxHealth: 90,   moveSpeed: SimFixed.FromInt(4),      armorClass: ArmorClass.Infantry, damageType: DamageType.Kinetic,    attackDamage: 10,  attackRangeTiles: 7,  attackCooldownTicks: 9),
            new SimUnitDefinition(13, FactionId.Alliance, UnitRole.AntiArmorInfantry,  costAE: 250,  buildTicks: 150, tier: 2, producerRole: UnitRole.Barracks,       maxHealth: 100,  moveSpeed: SimFixed.FromRaw(229376), armorClass: ArmorClass.Infantry, damageType: DamageType.Explosive,  attackDamage: 50,  attackRangeTiles: 10, attackCooldownTicks: 25), // 3.5
            new SimUnitDefinition(14, FactionId.Alliance, UnitRole.ScoutVehicle,       costAE: 300,  buildTicks: 120, tier: 1, producerRole: UnitRole.VehicleFactory, maxHealth: 220,  moveSpeed: SimFixed.FromInt(6),      armorClass: ArmorClass.Light,    damageType: DamageType.Kinetic,    attackDamage: 12,  attackRangeTiles: 8,  attackCooldownTicks: 10),
            new SimUnitDefinition(15, FactionId.Alliance, UnitRole.LightTank,          costAE: 600,  buildTicks: 200, tier: 1, producerRole: UnitRole.VehicleFactory, maxHealth: 550,  moveSpeed: SimFixed.FromInt(4),      armorClass: ArmorClass.Medium,   damageType: DamageType.Kinetic,    attackDamage: 35,  attackRangeTiles: 9,  attackCooldownTicks: 20),
            // Owner ruling (see D-074 consequences): BattleTank is Heavy, not the
            // Medium that ArmorSystem.md assigns it. ArmorSystem.md reserves Heavy
            // for the Heavy Tank, which MS-1 does not ship, which would have left
            // the Heavy column unexercised and the "Kinetic 0.25 vs Heavy forces
            // rockets" counter unreachable. Promoting the BattleTank is the
            // smallest change that makes that counter play in MS-1. The Legion
            // Koloss inherits the promotion so the faction columns stay parallel.
            new SimUnitDefinition(16, FactionId.Alliance, UnitRole.BattleTank,         costAE: 900,  buildTicks: 280, tier: 2, producerRole: UnitRole.VehicleFactory, maxHealth: 1100, moveSpeed: SimFixed.FromInt(3),      armorClass: ArmorClass.Heavy,    damageType: DamageType.Kinetic,    attackDamage: 60,  attackRangeTiles: 10, attackCooldownTicks: 25),
            new SimUnitDefinition(17, FactionId.Alliance, UnitRole.Artillery,          costAE: 1000, buildTicks: 320, tier: 2, producerRole: UnitRole.VehicleFactory, maxHealth: 350,  moveSpeed: SimFixed.FromRaw(163840), armorClass: ArmorClass.Light,    damageType: DamageType.Explosive,  attackDamage: 110, attackRangeTiles: 20, attackCooldownTicks: 70), // 2.5

            // --- Legion (factions[1]): Vehicles.md/Infantry.md concrete
            // costs, build times, HP and ranges; weapon damage/range/cadence
            // from Weapons.md where a Legion line exists (Gewehr 6–10 dmg /
            // 6 m / 1.0 s; Raketenwerfer 40–60 dmg / 9–11 m / 2.5 s — band
            // minimum, see class remarks) and from Vehicles.md where it
            // names a concrete Legion per-shot damage (Räuber 28, Koloss 50,
            // Donnerkanone 60 — D-075: the concrete GDD value beats the
            // derivation); the Scout alone keeps the integer-percent
            // derivation ((12 * 85) / 100 = 10). Cooldowns
            // equal the Alliance cadence (no GDD source differentiates it).
            // Koloss HP 1250 EXCEEDS the Alliance Aegis (1100) on purpose —
            // Vehicles.md names that concrete value; the derivation only
            // applies where the GDDs are silent. ---
            new SimUnitDefinition(18, FactionId.Legion,   UnitRole.Builder,            costAE: 650,  buildTicks: 240, tier: 1, producerRole: UnitRole.HQ,             maxHealth: 320,  moveSpeed: SimFixed.FromInt(3),      armorClass: ArmorClass.Light,    damageType: Unarmed,               attackDamage: 0,   attackRangeTiles: 0,  attackCooldownTicks: 0),
            new SimUnitDefinition(19, FactionId.Legion,   UnitRole.Harvester,          costAE: 550,  buildTicks: 200, tier: 1, producerRole: UnitRole.Refinery,       maxHealth: 750,  moveSpeed: SimFixed.FromRaw(163840), armorClass: ArmorClass.Light,    damageType: Unarmed,               attackDamage: 0,   attackRangeTiles: 0,  attackCooldownTicks: 0, cargoCapacityAE: 300), // 2.5; D-077: produced by the Refinery
            new SimUnitDefinition(29, FactionId.Legion,   UnitRole.BasicInfantry,      costAE: 60,   buildTicks: 80,  tier: 1, producerRole: UnitRole.Barracks,       maxHealth: 55,   moveSpeed: SimFixed.FromInt(4),      armorClass: ArmorClass.Infantry, damageType: DamageType.Kinetic,    attackDamage: 8,   attackRangeTiles: 6,  attackCooldownTicks: 10),
            new SimUnitDefinition(30, FactionId.Legion,   UnitRole.AntiArmorInfantry,  costAE: 200,  buildTicks: 120, tier: 2, producerRole: UnitRole.Barracks,       maxHealth: 90,   moveSpeed: SimFixed.FromRaw(229376), armorClass: ArmorClass.Infantry, damageType: DamageType.Explosive,  attackDamage: 40,  attackRangeTiles: 9,  attackCooldownTicks: 25), // 3.5
            new SimUnitDefinition(31, FactionId.Legion,   UnitRole.ScoutVehicle,       costAE: 220,  buildTicks: 90,  tier: 1, producerRole: UnitRole.VehicleFactory, maxHealth: 180,  moveSpeed: SimFixed.FromInt(6),      armorClass: ArmorClass.Light,    damageType: DamageType.Kinetic,    attackDamage: 10,  attackRangeTiles: 7,  attackCooldownTicks: 10),
            new SimUnitDefinition(32, FactionId.Legion,   UnitRole.LightTank,          costAE: 450,  buildTicks: 150, tier: 1, producerRole: UnitRole.VehicleFactory, maxHealth: 480,  moveSpeed: SimFixed.FromInt(4),      armorClass: ArmorClass.Medium,   damageType: DamageType.Kinetic,    attackDamage: 28,  attackRangeTiles: 8,  attackCooldownTicks: 20),
            new SimUnitDefinition(33, FactionId.Legion,   UnitRole.BattleTank,         costAE: 700,  buildTicks: 220, tier: 2, producerRole: UnitRole.VehicleFactory, maxHealth: 1250, moveSpeed: SimFixed.FromInt(3),      armorClass: ArmorClass.Heavy,    damageType: DamageType.Explosive,  attackDamage: 50,  attackRangeTiles: 8,  attackCooldownTicks: 25),
            new SimUnitDefinition(34, FactionId.Legion,   UnitRole.Artillery,          costAE: 800,  buildTicks: 260, tier: 2, producerRole: UnitRole.VehicleFactory, maxHealth: 320,  moveSpeed: SimFixed.FromRaw(163840), armorClass: ArmorClass.Light,    damageType: DamageType.Explosive,  attackDamage: 60,  attackRangeTiles: 18, attackCooldownTicks: 70), // 2.5
        };

        /// <summary>All building definitions (both factions), in table order. Read-only span over the static table.</summary>
        public static ReadOnlySpan<SimBuildingDefinition> AllBuildings => Buildings;

        /// <summary>All unit definitions (both factions), in table order. Read-only span over the static table.</summary>
        public static ReadOnlySpan<SimUnitDefinition> AllUnits => Units;

        /// <summary>
        /// The definition id of a faction's role under the documented id
        /// rule: Alliance ids ARE the <see cref="UnitRole"/> wire values
        /// (1..17), Legion ids add <see cref="FactionDefinitionOffset"/>
        /// (18..34). The generic <see cref="UnitRole.Unit"/> role (0) has no
        /// definition and maps to the invalid raw id 0.
        /// </summary>
        public static ushort ToDefinitionId(FactionId faction, UnitRole role)
        {
            int baseId = (int)role;
            if (baseId <= 0 || baseId > RolesPerFaction)
            {
                return 0;
            }
            return (ushort)(baseId + (faction == FactionId.Legion ? FactionDefinitionOffset : 0));
        }

        /// <summary>Looks up a building definition by its stable numeric id (either faction).</summary>
        public static bool TryGetBuilding(ushort definitionId, out SimBuildingDefinition definition)
        {
            for (int i = 0; i < Buildings.Length; i++)
            {
                if (Buildings[i].DefinitionId == definitionId)
                {
                    definition = Buildings[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }

        /// <summary>Looks up the building definition of one faction's building role.</summary>
        public static bool TryGetBuilding(FactionId faction, UnitRole role, out SimBuildingDefinition definition)
        {
            for (int i = 0; i < Buildings.Length; i++)
            {
                if (Buildings[i].Faction == faction && Buildings[i].Role == role)
                {
                    definition = Buildings[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }

        /// <summary>Looks up a unit definition by its stable numeric id (either faction).</summary>
        public static bool TryGetUnit(ushort definitionId, out SimUnitDefinition definition)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                if (Units[i].DefinitionId == definitionId)
                {
                    definition = Units[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }

        /// <summary>Looks up the unit definition of one faction's unit role.</summary>
        public static bool TryGetUnit(FactionId faction, UnitRole role, out SimUnitDefinition definition)
        {
            for (int i = 0; i < Units.Length; i++)
            {
                if (Units[i].Faction == faction && Units[i].Role == role)
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

        /// <summary>
        /// The canonical XXH64 content hash of the full 34-row definition
        /// table — the real <c>DefinitionsHash64</c> of the match fingerprint,
        /// replacing the pre-faction empty-content stub. XXH64 seed 0 in the
        /// NOVA_DEFINITIONS_V1 domain (SimulationCore.md section 5) over ids
        /// 1..<see cref="MaxDefinitionId"/> in ascending order; each
        /// definition contributes a field tag (its id) followed by a uniform
        /// 21-field layout in canonical order: kind u8 (0 = building, 1 =
        /// unit), faction u8, role u8, costAE i32, buildTicks i32,
        /// powerProvided i32, powerRequired i32, hasPrerequisite u8,
        /// prerequisiteRoles u32, tier u8, producerRole u8, maxHealth i32,
        /// moveSpeed raw i32, armorClass u8, damageType u8, attackDamage i32,
        /// attackRangeTiles i32, attackCooldownTicks i32, cargoCapacityAE i32.
        /// Fields a kind does
        /// not have hash as 0, so the layout is identical for every row and a
        /// value change anywhere — a weapon value included — moves the hash
        /// and therefore refuses replay start (SimulationCore.md section 6).
        /// <see cref="UnitState.SightRadius"/> is deliberately NOT a
        /// definition field today (it is a provisional per-entity default),
        /// so it takes no part in this hash; a future faction-resolved sight
        /// radius becomes definition content and then requires a NEW field
        /// generation in this layout, not a silent reuse of an existing one.
        /// </summary>
        public static ulong ComputeDefinitionsHash64()
        {
            return ComputeDefinitionsHash64(AllBuildings, AllUnits);
        }

        /// <summary>
        /// Hashes the given definition rows with the exact canonical layout
        /// of <see cref="ComputeDefinitionsHash64()"/>, iterating ids 1..
        /// <see cref="MaxDefinitionId"/> ascending regardless of span order
        /// (ordering is intrinsic, not caller duty). Exists so tests can
        /// prove sensitivity: a table copy with one mutated value must hash
        /// differently. Ids absent from both spans contribute nothing.
        /// </summary>
        public static ulong ComputeDefinitionsHash64(
            ReadOnlySpan<SimBuildingDefinition> buildings,
            ReadOnlySpan<SimUnitDefinition> units)
        {
            var hash = SimHashWriter.ForDefinitions();
            hash.WriteFieldTag(1);
            hash.WriteUInt32((uint)MaxDefinitionId);
            for (int id = 1; id <= MaxDefinitionId; id++)
            {
                if (TryFindBuilding(buildings, (ushort)id, out SimBuildingDefinition building))
                {
                    WriteBuildingRow(hash, in building);
                }
                else if (TryFindUnit(units, (ushort)id, out SimUnitDefinition unit))
                {
                    WriteUnitRow(hash, in unit);
                }
            }
            return hash.Digest();
        }

        private static void WriteBuildingRow(SimHashWriter hash, in SimBuildingDefinition def)
        {
            hash.WriteFieldTag(def.DefinitionId);
            hash.WriteUInt8(0); // kind: building
            hash.WriteUInt8((byte)def.Faction);
            hash.WriteUInt8((byte)def.Role);
            hash.WriteInt32(def.CostAE);
            hash.WriteInt32(def.BuildTicks);
            hash.WriteInt32(def.PowerProvided);
            hash.WriteInt32(def.PowerRequired);
            hash.WriteUInt8(def.HasPrerequisite ? (byte)1 : (byte)0);
            hash.WriteUInt32((uint)def.PrerequisiteRoles);
            hash.WriteUInt8(0); // tier: buildings have none
            hash.WriteUInt8(0); // producerRole: buildings have none
            hash.WriteInt32(def.MaxHealth);
            hash.WriteInt32(0); // moveSpeed raw: buildings have none
            hash.WriteUInt8((byte)def.ArmorClass);
            hash.WriteUInt8((byte)def.DamageType);
            hash.WriteInt32(def.AttackDamage);
            hash.WriteInt32(def.AttackRangeTiles);
            hash.WriteInt32(def.AttackCooldownTicks);
            hash.WriteInt32(0); // cargoCapacityAE: buildings have none
        }

        private static void WriteUnitRow(SimHashWriter hash, in SimUnitDefinition def)
        {
            hash.WriteFieldTag(def.DefinitionId);
            hash.WriteUInt8(1); // kind: unit
            hash.WriteUInt8((byte)def.Faction);
            hash.WriteUInt8((byte)def.Role);
            hash.WriteInt32(def.CostAE);
            hash.WriteInt32(def.BuildTicks);
            hash.WriteInt32(0); // powerProvided: units have none
            hash.WriteInt32(0); // powerRequired: units have none
            hash.WriteUInt8(0); // hasPrerequisite: units have none
            hash.WriteUInt32(0); // prerequisiteRoles: units have none
            hash.WriteUInt8(def.Tier);
            hash.WriteUInt8((byte)def.ProducerRole);
            hash.WriteInt32(def.MaxHealth);
            hash.WriteInt32(def.MoveSpeed.RawValue);
            hash.WriteUInt8((byte)def.ArmorClass);
            hash.WriteUInt8((byte)def.DamageType);
            hash.WriteInt32(def.AttackDamage);
            hash.WriteInt32(def.AttackRangeTiles);
            hash.WriteInt32(def.AttackCooldownTicks);
            hash.WriteInt32(def.CargoCapacityAE);
        }

        private static bool TryFindBuilding(ReadOnlySpan<SimBuildingDefinition> buildings, ushort id, out SimBuildingDefinition definition)
        {
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i].DefinitionId == id)
                {
                    definition = buildings[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }

        private static bool TryFindUnit(ReadOnlySpan<SimUnitDefinition> units, ushort id, out SimUnitDefinition definition)
        {
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i].DefinitionId == id)
                {
                    definition = units[i];
                    return true;
                }
            }
            definition = default;
            return false;
        }
    }
}

using System;
using Nova.Core;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Production;
using Nova.Simulation.State;

namespace Nova.Gameplay
{
    /// <summary>
    /// The buttons one command card can show. Presence is role-driven
    /// (<see cref="CommandCardPresenter"/>); whether a present button is
    /// clickable is a separate, state-driven evaluation
    /// (<see cref="ProductionBlocker"/>, <see cref="BuildingPlacementBlocker"/>,
    /// <see cref="BuildingRepairBlocker"/>)
    /// — a greyed button must always carry its reason.
    /// </summary>
    [Flags]
    public enum CommandButtonType
    {
        None = 0,
        Move = 1 << 0,
        Stop = 1 << 1,
        Attack = 1 << 2,
        Build = 1 << 3,
        Harvest = 1 << 4,
        ReturnCargo = 1 << 5,
        Repair = 1 << 6,
        Sell = 1 << 7,
        CancelConstruction = 1 << 8,
        InstallDefenseModule = 1 << 9,
    }

    /// <summary>
    /// Why a production button is blocked, in the sim's own validation order
    /// (mirror of <see cref="ProductionSystem.ValidateQueueUnit"/>): the T2
    /// gate first, then the queue capacity, then affordability. The HUD
    /// greys with the FIRST blocker, so the reason the player reads is the
    /// same one the executor would reject with at the target tick.
    /// </summary>
    public enum ProductionBlocker
    {
        None = 0,
        TierLocked = 1,
        QueueFull = 2,
        InsufficientCredits = 3,
    }

    /// <summary>
    /// Why the build bar reports a building as blocked. Before a target cell
    /// exists, the HUD uses the explicit priority prerequisite, affordability,
    /// free power and finally global construction-site capacity. This is a UI
    /// priority, not the global CommandExecutor order (which checks credits
    /// before domain validation). The UI derives the reason because schema v1
    /// deliberately shares one result code between several domain cases.
    /// </summary>
    public enum BuildingPlacementBlocker
    {
        None = 0,
        MissingPrerequisite = 1,
        InsufficientCredits = 2,
        InsufficientPower = 3,
        SiteCapacityReached = 4,
    }

    /// <summary>
    /// Why a building's repair button is blocked. The sim's repair order
    /// needs a live Builder as the actor and an own COMPLETED placement that
    /// is damaged (<see cref="Construction.ConstructionSystem.ValidateRepair"/>)
    /// — an undamaged building has nothing to repair, and without a Builder
    /// there is no actor to assign.
    /// </summary>
    public enum BuildingRepairBlocker
    {
        None = 0,
        NotDamaged = 1,
        NoBuilderAvailable = 2,
    }

    /// <summary>
    /// The testable brain of the command card: maps the selection to the
    /// buttons the card shows (presence) and evaluates state-driven
    /// blockers (availability), so the IMGUI component one layer up only
    /// renders and dispatches. Plain Unity-free class on purpose — the same
    /// precedent as <see cref="RtsIntentDispatcher"/>: no MonoBehaviour, no
    /// UnityEngine types, so EditMode tests cover the whole mapping.
    /// <para>
    /// PRESENCE RULES (schema v1, MS-1): a mobile lead unit always offers
    /// Move/Stop; Attack only when the role is ARMED (its definition row
    /// carries AttackDamage &gt; 0 — an unarmed Builder or Harvester would
    /// accept the order and then never fire, which is a lie on a button);
    /// the Harvester adds Harvest/ReturnCargo, the Builder adds Repair. A
    /// completed building offers Sell and Repair; a construction site offers
    /// CancelConstruction instead. <see cref="CommandButtonType.InstallDefenseModule"/>
    /// is returned as PRESENT for the DefensePlatform so the HUD can show it
    /// deliberately disabled — the schema-v1 kind exists but the sim rejects
    /// it with RejectedPrerequisitesNotMet in this slice (defense modules are
    /// G2/G4 content), so it must never be dispatched.
    /// </para>
    /// <para>
    /// NOT REPRESENTABLE in schema v1 (open design questions, deliberately
    /// not improvised): research upgrades (mvp-v1.json disables them) and
    /// tower target priorities. No CommandKind exists for either, so no
    /// button exists either.
    /// </para>
    /// <para>
    /// REPAIR ACTOR RULE: the sim issues repair as a BUILDER-side order on
    /// an own damaged completed building (<see cref="RtsIntentDispatcher.Repair"/>).
    /// For the building-side card button there is no selected Builder, so the
    /// actor is chosen by <see cref="TryFindRepairBuilder"/> — the same
    /// lowest-index own Builder convention the sim itself uses for a site's
    /// auto-assignment (<see cref="Construction.ConstructionSystem"/>),
    /// rebuilt here from the entity store so the UI cannot drift from it.
    /// </para>
    /// </summary>
    public sealed class CommandCardPresenter
    {
        /// <summary>
        /// Legacy count-only mapping of the first graybox slice (pinned by
        /// SelectionManagerTests): any nonempty selection gets the combat
        /// defaults. Kept for callers that know nothing but a count; new code
        /// should use the role-aware <see cref="GetUnitCommands"/>.
        /// </summary>
        public CommandButtonType GetAvailableCommands(int selectedCount)
        {
            if (selectedCount <= 0) return CommandButtonType.None;

            // Combat units support Move, Stop, and Attack commands
            return CommandButtonType.Move | CommandButtonType.Stop | CommandButtonType.Attack;
        }

        /// <summary>
        /// The command buttons of a MOBILE lead unit (the unit card). Building
        /// roles return <see cref="CommandButtonType.None"/> — their card is
        /// <see cref="GetBuildingCommands"/> — and a generic
        /// <see cref="UnitRole.Unit"/> (no definition row, e.g. a role-less
        /// spawn) is treated as unarmed: Move/Stop only.
        /// </summary>
        public CommandButtonType GetUnitCommands(FactionId faction, UnitRole leadRole)
        {
            if (SimDefinitions.IsBuildingRole(leadRole)) return CommandButtonType.None;

            CommandButtonType commands = CommandButtonType.Move | CommandButtonType.Stop;
            bool armed = SimDefinitions.TryGetUnit(faction, leadRole, out SimUnitDefinition definition)
                && definition.AttackDamage > 0;
            if (armed) commands |= CommandButtonType.Attack;

            switch (leadRole)
            {
                case UnitRole.Harvester:
                    commands |= CommandButtonType.Harvest | CommandButtonType.ReturnCargo;
                    break;
                case UnitRole.Builder:
                    commands |= CommandButtonType.Repair;
                    break;
            }
            return commands;
        }

        /// <summary>The command buttons of a CONSTRUCTION SITE (definition role with an active site-register row): only cancelling is meaningful.</summary>
        public CommandButtonType GetSiteCommands()
        {
            return CommandButtonType.CancelConstruction;
        }

        /// <summary>
        /// The command buttons of a COMPLETED own building: Sell and Repair
        /// for every building role, plus <see cref="CommandButtonType.InstallDefenseModule"/>
        /// for the DefensePlatform — present so the HUD can grey it with the
        /// honest reason, never so it can be dispatched (see class remarks).
        /// </summary>
        public CommandButtonType GetBuildingCommands(UnitRole buildingRole)
        {
            if (!SimDefinitions.IsBuildingRole(buildingRole)) return CommandButtonType.None;

            CommandButtonType commands = CommandButtonType.Sell | CommandButtonType.Repair;
            if (buildingRole == UnitRole.DefensePlatform)
            {
                commands |= CommandButtonType.InstallDefenseModule;
            }
            return commands;
        }

        /// <summary>
        /// The unit definitions a producer building can queue, in definition
        /// table order: <see cref="SimDefinitions.AllUnits"/> filtered by
        /// faction and <see cref="SimUnitDefinition.ProducerRole"/> — the
        /// same table the executor validates against, so the buttons cannot
        /// drift from it. Returns the count written into
        /// <paramref name="destination"/> (capped at its length).
        /// </summary>
        public int GetProducibleUnits(FactionId faction, UnitRole producerRole, SimUnitDefinition[] destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            int written = 0;
            ReadOnlySpan<SimUnitDefinition> units = SimDefinitions.AllUnits;
            for (int i = 0; i < units.Length && written < destination.Length; i++)
            {
                if (units[i].Faction == faction && units[i].ProducerRole == producerRole)
                {
                    destination[written++] = units[i];
                }
            }
            return written;
        }

        /// <summary>
        /// First blocker of a production button, in the executor's own
        /// validation order (T2 gate, then queue capacity at
        /// <see cref="ProductionSystem.MaxQueueEntries"/>, then affordability
        /// of one unit). The producer/faction match itself is guaranteed by
        /// <see cref="GetProducibleUnits"/> and needs no re-check here.
        /// </summary>
        public static ProductionBlocker EvaluateProductionBlocker(
            in SimUnitDefinition definition, long credits, bool t2Unlocked, int queueEntryCount)
        {
            if (definition.Tier >= 2 && !t2Unlocked) return ProductionBlocker.TierLocked;
            if (queueEntryCount >= ProductionSystem.MaxQueueEntries) return ProductionBlocker.QueueFull;
            if (credits < definition.CostAE) return ProductionBlocker.InsufficientCredits;
            return ProductionBlocker.None;
        }

        /// <summary>
        /// First building-placement blocker in the build bar's explicit UI
        /// priority. Geometry is intentionally absent: the bar has no target
        /// cell until placement mode starts. This priority is not the global
        /// CommandExecutor order; an energy blocker is informational and must
        /// not disable entering placement mode.
        /// </summary>
        public static BuildingPlacementBlocker EvaluateBuildingPlacementBlocker(
            in SimBuildingDefinition definition, bool prerequisiteMet, long credits,
            int powerProvided, int powerRequired, int activeSiteCount)
        {
            if (!prerequisiteMet) return BuildingPlacementBlocker.MissingPrerequisite;
            if (credits < definition.CostAE) return BuildingPlacementBlocker.InsufficientCredits;
            if (definition.PowerRequired > 0
                && powerProvided - powerRequired < definition.PowerRequired)
            {
                return BuildingPlacementBlocker.InsufficientPower;
            }
            if (activeSiteCount >= ConstructionSystem.MaxSites)
            {
                return BuildingPlacementBlocker.SiteCapacityReached;
            }
            return BuildingPlacementBlocker.None;
        }

        /// <summary>
        /// Compact live grid balance for the build bar. Low power must name
        /// its gameplay consequence where the player makes build decisions.
        /// </summary>
        public static string FormatPowerBalance(int powerProvided, int powerRequired)
        {
            string balance = $"Strom {powerProvided}/{powerRequired}";
            return powerRequired > powerProvided
                ? balance + " · LOW POWER: Produktion ½"
                : balance;
        }

        /// <summary>Power generation or draw shown on a selected building's command card and on build-button hover.</summary>
        public static string FormatBuildingPower(in SimBuildingDefinition definition)
        {
            if (definition.PowerProvided > 0 && definition.PowerRequired > 0)
            {
                return $"Erzeugt +{definition.PowerProvided} · benötigt {definition.PowerRequired} Strom";
            }
            if (definition.PowerProvided > 0) return $"Erzeugt +{definition.PowerProvided} Strom";
            if (definition.PowerRequired > 0) return $"Benötigt {definition.PowerRequired} Strom";
            return "Kein Strombedarf";
        }

        /// <summary>
        /// First blocker of a building's repair button: an undamaged building
        /// has nothing to repair (the executor rejects such an order as an
        /// invalid target), and without a live own Builder there is no actor
        /// to assign the standing repair order to.
        /// </summary>
        public static BuildingRepairBlocker EvaluateBuildingRepairBlocker(bool isDamaged, bool builderAvailable)
        {
            if (!isDamaged) return BuildingRepairBlocker.NotDamaged;
            if (!builderAvailable) return BuildingRepairBlocker.NoBuilderAvailable;
            return BuildingRepairBlocker.None;
        }

        /// <summary>
        /// The building-side repair actor: the lowest-index living own
        /// Builder, the sim's own documented auto-assignment convention for
        /// construction sites (ascending-index scan, deterministic). Rebuilt
        /// here from the entity store because the sim's scan is private to
        /// <see cref="Construction.ConstructionSystem"/>.
        /// </summary>
        public static bool TryFindRepairBuilder(EntityManager entities, byte playerSlot, out EntityId builder)
        {
            builder = EntityId.Invalid;
            if (entities == null) return false;

            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (unit.IsActive && unit.Role == UnitRole.Builder && unit.PlayerId == playerSlot)
                {
                    builder = unit.Id;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// German display names of the eight MS-1 unit roles per faction,
        /// from the canonical MS-1 name tables of docs/gamedesign/Vehicles.md
        /// and docs/gamedesign/Infantry.md (runbook/GDD language). Roles
        /// outside the unit roster (buildings, the generic Unit role) fall
        /// back to the enum name.
        /// </summary>
        public static string UnitDisplayName(FactionId faction, UnitRole role)
        {
            bool legion = faction == FactionId.Legion;
            switch (role)
            {
                case UnitRole.Builder: return legion ? "Vorarbeiter" : "Pionier „Atlas“";
                case UnitRole.Harvester: return legion ? "Schürfer" : "Sammler „Demeter“";
                case UnitRole.BasicInfantry: return legion ? "Rekrut" : "Rifleman";
                case UnitRole.AntiArmorInfantry: return legion ? "Raketenschütze" : "Rocket Soldier";
                case UnitRole.ScoutVehicle: return legion ? "Hyäne (Buggy)" : "Jackal-Aufklärer";
                case UnitRole.LightTank: return legion ? "Räuber" : "Lynx";
                case UnitRole.BattleTank: return legion ? "Koloss" : "Aegis";
                case UnitRole.Artillery: return legion ? "Donnerkanone" : "Longbow";
                default: return role.ToString();
            }
        }

        /// <summary>German display names of the nine MS-1 building roles (runbook/GDD language); shared by the build bar and the command card.</summary>
        public static string BuildingDisplayName(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.HQ: return "Hauptquartier";
                case UnitRole.Refinery: return "Raffinerie";
                case UnitRole.Power: return "Kraftwerk";
                case UnitRole.Storage: return "Lager";
                case UnitRole.Barracks: return "Kaserne";
                case UnitRole.VehicleFactory: return "Fahrzeugfabrik";
                case UnitRole.ResearchLab: return "Forschungslabor";
                case UnitRole.Radar: return "Radar";
                case UnitRole.DefensePlatform: return "Verteidigungsplattform";
                default: return role.ToString();
            }
        }
    }
}

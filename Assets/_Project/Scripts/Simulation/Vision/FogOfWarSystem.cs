using System;
using System.Collections.Generic;
using Nova.Core;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Vision
{
    /// <summary>
    /// Canonical Fog of War system (docs/tech/FogOfWar.md, D-058): the single
    /// authoritative sight for Combat legality, AI, the player snapshot and
    /// rendering. Holds one committed 1-m-grid mask per team with the three
    /// states <see cref="VisionState.Unexplored"/>/<see cref="VisionState.Explored"/>/
    /// <see cref="VisionState.Visible"/> and recomputes them at 5 Hz — on every
    /// second simulation tick, after Movement and before Combat
    /// (SimulationCore.md section 2, order step 7).
    /// <para>
    /// Tick cadence: the kernel calls every system on every tick; the 5 Hz
    /// decision is made internally (<c>tick % <see cref="RecomputeIntervalTicks"/> == 0</c>).
    /// Between two recomputes the last committed mask stays in force and is
    /// the ONLY sight any consumer may use — the buffers are private to this
    /// system and no API exposes a provisional or self-computed view.
    /// </para>
    /// <para>
    /// MS-1 sight model: pure radii — no blockers, heights, weather,
    /// stealth/detection or special per-unit sight logic. Radar produces
    /// minimap <see cref="RadarSignature"/> pings WITHOUT targeting
    /// permission, and only from a COMPLETED Radar building (16.5, #54, C3):
    /// the building is the radiator, its own sight radius times
    /// <see cref="RadarRadiusMultiplier"/> is the coverage — no finished
    /// Radar, no coverage. At a power deficit the radar is the FIRST system
    /// to fall (16.6, C4, Economy.md Low-Power rule): no coverage, no pings. MS-1 simplification:
    /// team index equals the player slot (D-058 activates exactly two slots;
    /// allied shared sight is post-MS-1).
    /// </para>
    /// <para>
    /// Determinism (FogOfWar.md section 5): integer distance tests in stable
    /// ascending entity-index order, fixed team and cell iteration. The
    /// committed masks and the last recompute tick are authoritative state
    /// and serialize into snapshot block <see cref="SnapshotBlockIds.FogOfWar"/>,
    /// so a single changed cell changes the canonical state hash.
    /// </para>
    /// </summary>
    public sealed class FogOfWarSystem : IStatefulSimSystem
    {
        /// <summary>Serialization version of the Fog of War snapshot block.</summary>
        public const byte StateVersion = 1;

        /// <summary>Format capacity for team masks (D-058 reserves eight slots; MS-1 activates two).</summary>
        public const int MaxTeams = 8;

        /// <summary>Recompute every second tick: 5 Hz on the canonical 10 Hz clock.</summary>
        public const int RecomputeIntervalTicks = 2;

        /// <summary>
        /// Radar coverage factor over the building's sight radius (16.5,
        /// #54, C3: only a COMPLETED Radar building radiates — the finished
        /// building's own sight radius times this factor is the coverage.
        /// The pre-16.5 rule, every unit radiating at twice its sight, is
        /// gone together with its last caller contract).
        /// </summary>
        public const int RadarRadiusMultiplier = 2;

        private readonly EntityManager _entityManager;
        private readonly Construction.ConstructionSystem _construction;
        private readonly Economy.EconomySystem _economy;
        private readonly byte[][] _masks;
        private readonly HashSet<int> _radarSeenCells = new HashSet<int>();

        public string Name => "FogOfWarSystem";

        public ushort Width { get; }
        public ushort Height { get; }

        /// <summary>Active team slots (MS-1: exactly 2, D-058).</summary>
        public int TeamCount { get; }

        /// <summary>Tick of the last committed recompute; meaningful only when <see cref="HasCommittedView"/>.</summary>
        public uint LastRecomputeTick { get; private set; }

        /// <summary>False until the first recompute committed a view (all cells Unexplored).</summary>
        public bool HasCommittedView { get; private set; }

        /// <summary>
        /// The construction system is a REQUIRED dependency since 16.5 (#54):
        /// radar coverage derives from COMPLETED Radar placements, which only
        /// its register can tell from sites and corpses. The economy is one
        /// since 16.6 (C4, Economy.md Low-Power rule): at a power deficit the radar goes OFFLINE —
        /// no pings, no coverage. Both are read-only here (placement queries,
        /// balance reads) and never ticked through this system.
        /// </summary>
        public FogOfWarSystem(EntityManager entityManager, Construction.ConstructionSystem construction, Economy.EconomySystem economy, int teamCount = 2, ushort width = 128, ushort height = 128)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            if (teamCount < 1 || teamCount > MaxTeams)
            {
                throw new ArgumentOutOfRangeException(nameof(teamCount), teamCount, $"Team count must be in [1, {MaxTeams}].");
            }
            if (width == 0 || height == 0)
            {
                throw new ArgumentException("Grid dimensions must be greater than zero.");
            }

            Width = width;
            Height = height;
            TeamCount = teamCount;

            // Simplest correct storage: one byte per cell per team
            // (16 KiB per team at 128x128; the 2-bit packing is a post-MS-1
            // optimization and must not change a single hash byte).
            _masks = new byte[teamCount][];
            for (int t = 0; t < teamCount; t++)
            {
                _masks[t] = new byte[width * height];
            }
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo($"[{Name}] Initialized canonical Fog of War ({Width}x{Height}, {TeamCount} teams, 5 Hz).");
        }

        /// <summary>
        /// Runs the 5 Hz recompute on every second tick; odd ticks leave the
        /// committed masks untouched. Registered after Movement and before
        /// Combat, so the recompute sees the final positions of this tick
        /// (SimulationCore.md section 2).
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            if (tick.Value % RecomputeIntervalTicks != 0) return;
            Recompute(tick);
        }

        public void Shutdown()
        {
        }

        /// <summary>
        /// The committed view of one team — the only sight Combat, AI, the
        /// player snapshot and rendering may consume.
        /// </summary>
        public TeamView GetTeamView(byte team)
        {
            if (team >= TeamCount)
            {
                throw new ArgumentOutOfRangeException(nameof(team), team, "Unknown team slot.");
            }
            return new TeamView(_masks[team], Width, Height);
        }

        /// <summary>
        /// Entities visible to a team under the committed mask (FogOfWar.md
        /// section 4): the team's own units always, foreign units only while
        /// their grid cell is <see cref="VisionState.Visible"/>. Appended in
        /// ascending entity-index order; returns the number appended.
        /// <para>
        /// Known MS-1 limitation (index side channel): own
        /// <see cref="EntityId"/> values reveal the shared allocator's
        /// assignment order, so an observer can infer rough spawn timing of
        /// hidden enemy entities from id gaps. The privacy boundary of
        /// FogOfWar.md section 4 sits at the view/filter level; id metadata
        /// is not part of the MS-1 privacy model. Hardening (opaque or
        /// per-team id mapping) is a post-MS-1 topic.
        /// </para>
        /// </summary>
        public int GetVisibleEntities(byte team, List<EntityId> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (team >= TeamCount)
            {
                throw new ArgumentOutOfRangeException(nameof(team), team, "Unknown team slot.");
            }

            byte[] mask = _masks[team];
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            int appended = 0;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive) continue;
                if (u.PlayerId == team)
                {
                    results.Add(u.Id);
                    appended++;
                    continue;
                }
                int gx = GridXOf(in u);
                int gy = GridYOf(in u);
                if (mask[gy * Width + gx] == (byte)VisionState.Visible)
                {
                    results.Add(u.Id);
                    appended++;
                }
            }
            return appended;
        }

        /// <summary>
        /// Radar minimap pings (FogOfWar.md section 3): grid positions of
        /// foreign units inside the team's radar coverage whose cell is NOT
        /// <see cref="VisionState.Visible"/> — a visible enemy is a legal
        /// target, not a signature. Signatures carry no entity reference and
        /// grant no targeting permission. Deterministic: ascending entity
        /// index, one ping per cell. Derived view over committed sight and
        /// current positions; not part of the authoritative state.
        /// <para>
        /// Coverage (16.5, #54, C3): only a COMPLETED Radar building of the
        /// team radiates, over the building's own sight radius times
        /// <see cref="RadarRadiusMultiplier"/>. A Radar site, a destroyed one
        /// and every other entity radiate nothing — without a finished Radar
        /// the team has no coverage at all (and MinimapHud draws no map). At
        /// a power deficit the radar is the FIRST system to fall (16.6, C4,
        /// Economy.md Low-Power rule): this method returns nothing.
        /// </para>
        /// <para>
        /// Cadence (Q-040(j), provisional): pings derive from live 10 Hz
        /// positions and can fire before the first committed view — FogOfWar.md
        /// section 6.3 pins only the <see cref="VisionState.Visible"/> cadence
        /// to the 5 Hz commit tick, not the radar cadence. Candidate under
        /// review: bind pings to the same 5 Hz commit ticks. Until
        /// ratification the provisional behavior is intentional.
        /// </para>
        /// </summary>
        public int GetRadarSignatures(byte team, List<RadarSignature> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (team >= TeamCount)
            {
                throw new ArgumentOutOfRangeException(nameof(team), team, "Unknown team slot.");
            }

            // 16.6 (C4, Economy.md Low-Power rule): at a power deficit the radar is the FIRST
            // system to fall — no coverage, no pings. The minimap reads the
            // same balance and goes dark with it.
            if (_economy.GetPlayerEconomy(team).IsLowPower)
            {
                return 0;
            }

            byte[] mask = _masks[team];
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            int appended = 0;
            _radarSeenCells.Clear();

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState target = ref units[i];
                if (!target.IsActive || target.PlayerId == team) continue;

                int gx = GridXOf(in target);
                int gy = GridYOf(in target);
                int cell = gy * Width + gx;
                if (mask[cell] == (byte)VisionState.Visible) continue; // a target, not a ping
                if (!_radarSeenCells.Add(cell)) continue; // one signature per cell

                // Radar coverage: any own COMPLETED Radar building whose
                // radar radius reaches the target's cell center (same
                // cell-center rule as sight).
                for (int j = 0; j < capacity; j++)
                {
                    ref readonly UnitState observer = ref units[j];
                    if (!observer.IsActive || observer.PlayerId != team) continue;
                    if (observer.Role != UnitRole.Radar) continue;
                    if (!_construction.IsCompletedPlacement(UnitCommandStateView.ToRawEntityId(observer.Id))) continue;

                    SimFixed radarRadius = observer.SightRadius * SimFixed.FromInt(RadarRadiusMultiplier);
                    int ox = GridXOf(in observer);
                    int oy = GridYOf(in observer);
                    if (CellCenterWithinRadius(gx, gy, ox, oy, radarRadius))
                    {
                        results.Add(new RadarSignature(gx, gy));
                        appended++;
                        break;
                    }
                }
            }
            return appended;
        }

        /// <summary>
        /// The 5 Hz recompute (FogOfWar.md sections 1 and 5): demote last
        /// tick's Visible cells to Explored per team (ascending team index),
        /// then rasterize every living unit's sight circle in stable
        /// ascending entity-index order. Unexplored is only ever left via
        /// Visible; Explored never falls back.
        /// </summary>
        private void Recompute(Tick tick)
        {
            for (int t = 0; t < TeamCount; t++)
            {
                byte[] mask = _masks[t];
                for (int i = 0; i < mask.Length; i++)
                {
                    if (mask[i] == (byte)VisionState.Visible)
                    {
                        mask[i] = (byte)VisionState.Explored;
                    }
                }
            }

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId >= TeamCount) continue;
                RevealCircle(u.PlayerId, in u);
            }

            LastRecomputeTick = tick.Value;
            HasCommittedView = true;
        }

        /// <summary>
        /// Rasterizes one unit's sight circle into its team's mask. Circle
        /// anchor: the unit's canonical grid position
        /// (<see cref="SimFixed.WorldToGrid"/>, floor). Rounding rule: a cell
        /// is visible when its center lies within the radius — since cell
        /// centers sit at half-integer world coordinates, their offset to the
        /// anchor cell's center is a whole number of cells, so the test
        /// compares (dx^2 + dy^2) against radius^2 exactly in Q16.16
        /// (both sides scaled to Q32.32). No radius rounding occurs; a cell
        /// at exactly radius distance is visible (&lt;=).
        /// </summary>
        private void RevealCircle(byte team, in UnitState unit)
        {
            SimFixed radius = unit.SightRadius;
            if (radius.RawValue < 0)
            {
                // Out-of-domain content is a deterministic checked fault
                // (SimulationCore.md section 1), never silent saturation.
                throw new InvalidOperationException(
                    $"Negative sight radius on entity {unit.Id}; the FoW recompute cannot proceed deterministically.");
            }

            int gx = GridXOf(in unit);
            int gy = GridYOf(in unit);

            // Conservative iteration bound: ceil(radius) in whole cells.
            int rCeil = (int)(((long)radius.RawValue + (SimFixed.OneRaw - 1)) >> SimFixed.FractionalBits);
            int minX = Math.Max(0, gx - rCeil);
            int maxX = Math.Min(Width - 1, gx + rCeil);
            int minY = Math.Max(0, gy - rCeil);
            int maxY = Math.Min(Height - 1, gy + rCeil);

            byte[] mask = _masks[team];
            for (int y = minY; y <= maxY; y++)
            {
                int row = y * Width;
                for (int x = minX; x <= maxX; x++)
                {
                    if (CellCenterWithinRadius(x, y, gx, gy, radius))
                    {
                        mask[row + x] = (byte)VisionState.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// Exact cell-center distance test in fixed point:
        /// (dx^2 + dy^2) &lt;= radius^2, both sides in Q32.32.
        /// </summary>
        private static bool CellCenterWithinRadius(int cellX, int cellY, int originX, int originY, SimFixed radius)
        {
            long dx = cellX - originX;
            long dy = cellY - originY;
            long distSq = (dx * dx + dy * dy) << (2 * SimFixed.FractionalBits);
            long radiusSq = (long)radius.RawValue * radius.RawValue;
            return distSq <= radiusSq;
        }

        private int GridXOf(in UnitState unit)
        {
            return Math.Max(0, Math.Min(Width - 1, SimFixed.WorldToGrid(unit.Transform.PositionX)));
        }

        private int GridYOf(in UnitState unit)
        {
            return Math.Max(0, Math.Min(Height - 1, SimFixed.WorldToGrid(unit.Transform.PositionY)));
        }

        /// <summary>Snapshot block id of the Fog of War state (registry: <see cref="SnapshotBlockIds"/>; assignment provisional per Q-040).</summary>
        public ushort StateBlockId => SnapshotBlockIds.FogOfWar;

        /// <summary>
        /// Block content v1: version, grid dimensions, team count, the last
        /// recompute tick with its committed flag, then every team's mask in
        /// ascending team order as raw cell bytes (row-major, y * width + x).
        /// Hash-sensitive by construction: any single cell change moves the
        /// block bytes and therefore the canonical state hash.
        /// </summary>
        public void WriteState(SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);
            writer.WriteUInt16(Width);
            writer.WriteUInt16(Height);
            writer.WriteUInt8(unchecked((byte)TeamCount));
            writer.WriteUInt8(HasCommittedView ? (byte)1 : (byte)0);
            writer.WriteUInt32(LastRecomputeTick);
            for (int t = 0; t < TeamCount; t++)
            {
                writer.WriteBytes(_masks[t]);
            }
        }

        /// <summary>Fully validates a Fog of War block without mutating the masks.</summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _);
        }

        /// <summary>
        /// Restores the committed masks and recompute tick; malformed input
        /// returns false and leaves every mask untouched (two-phase contract
        /// of <see cref="IStatefulSimSystem"/>).
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out ParsedFogOfWar parsed)) return false;

            for (int t = 0; t < TeamCount; t++)
            {
                Array.Copy(parsed.Masks[t], _masks[t], _masks[t].Length);
            }
            LastRecomputeTick = parsed.LastRecomputeTick;
            HasCommittedView = parsed.HasCommittedView;
            return true;
        }

        /// <summary>Validated intermediate of a parsed Fog of War block.</summary>
        private sealed class ParsedFogOfWar
        {
            public byte[][] Masks;
            public uint LastRecomputeTick;
            public bool HasCommittedView;
        }

        /// <summary>
        /// Parses and fully validates block content — exact lengths, matching
        /// dimensions/team count, legal cell values — into a commit-ready
        /// intermediate. Never mutates this system.
        /// </summary>
        private bool TryParseState(ReadOnlySpan<byte> blockContent, out ParsedFogOfWar parsed)
        {
            parsed = null;
            var reader = new SnapshotBlockReader(blockContent);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt16(out ushort width) || width != Width) return false;
            if (!reader.TryReadUInt16(out ushort height) || height != Height) return false;
            if (!reader.TryReadUInt8(out byte teamCount) || teamCount != TeamCount) return false;
            if (!reader.TryReadUInt8(out byte committedRaw) || committedRaw > 1) return false;
            if (!reader.TryReadUInt32(out uint lastRecomputeTick)) return false;

            int cellCount = width * height;
            var masks = new byte[teamCount][];
            for (int t = 0; t < teamCount; t++)
            {
                if (!reader.TryReadBytes(cellCount, out ReadOnlySpan<byte> cells)) return false;
                var mask = new byte[cellCount];
                for (int i = 0; i < cellCount; i++)
                {
                    if (cells[i] > (byte)VisionState.Visible) return false; // only the three legal states
                    mask[i] = cells[i];
                }
                masks[t] = mask;
            }
            if (reader.Remaining != 0) return false;

            parsed = new ParsedFogOfWar
            {
                Masks = masks,
                LastRecomputeTick = lastRecomputeTick,
                HasCommittedView = committedRaw == 1
            };
            return true;
        }
    }
}

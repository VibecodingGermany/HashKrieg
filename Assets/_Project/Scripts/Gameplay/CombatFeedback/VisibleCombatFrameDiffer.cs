using System;
using System.Collections.Generic;
using Nova.Core;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using UnityEngine;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.CombatFeedback
{
    /// <summary>
    /// Presentation events that can be reconstructed from two consecutive,
    /// locally visible simulation snapshots. They are cosmetic observations,
    /// never authoritative game events.
    /// </summary>
    public enum CombatFeedbackKind : byte
    {
        Shot,
        Hit,
        Death,
        UnitReady,
    }

    /// <summary>
    /// The fog-safe slice of one visible entity retained between rendered
    /// frames. The caller must build samples exclusively from the committed
    /// team view; the differ deliberately has no entity-store dependency.
    /// </summary>
    public readonly struct VisibleCombatSample
    {
        public EntityId Id { get; }
        public byte PlayerId { get; }
        public UnitRole Role { get; }
        public Vector3 Position { get; }
        public int CurrentHealth { get; }
        public int WeaponCooldownTicks { get; }
        public EntityId AttackTarget { get; }
        public DamageType DamageType { get; }

        public VisibleCombatSample(
            EntityId id,
            byte playerId,
            UnitRole role,
            Vector3 position,
            int currentHealth,
            int weaponCooldownTicks,
            EntityId attackTarget,
            DamageType damageType)
        {
            Id = id;
            PlayerId = playerId;
            Role = role;
            Position = position;
            CurrentHealth = currentHealth;
            WeaponCooldownTicks = weaponCooldownTicks;
            // EntityId's all-zero struct reports Index >= 0 even though the
            // entity store starts generations at 1. Normalize that language
            // default so an omitted target can never fabricate a shot at 0:0.
            AttackTarget = attackTarget.Version == 0 ? EntityId.Invalid : attackTarget;
            DamageType = damageType;
        }
    }

    /// <summary>
    /// One cosmetic cue. A target position is optional because hidden or
    /// already-despawned targets must never be resolved through the raw entity
    /// store merely to draw a tracer.
    /// </summary>
    public readonly struct CombatFeedbackEvent
    {
        public CombatFeedbackKind Kind { get; }
        public EntityId SourceId { get; }
        public EntityId TargetId { get; }
        public Vector3 SourcePosition { get; }
        public Vector3 TargetPosition { get; }
        public UnitRole SourceRole { get; }
        public UnitRole TargetRole { get; }
        public DamageType DamageType { get; }
        public bool HasTargetPosition { get; }

        public CombatFeedbackEvent(
            CombatFeedbackKind kind,
            EntityId sourceId,
            EntityId targetId,
            Vector3 sourcePosition,
            Vector3 targetPosition,
            UnitRole sourceRole,
            UnitRole targetRole,
            DamageType damageType,
            bool hasTargetPosition)
        {
            Kind = kind;
            SourceId = sourceId;
            TargetId = targetId;
            SourcePosition = sourcePosition;
            TargetPosition = targetPosition;
            SourceRole = sourceRole;
            TargetRole = targetRole;
            DamageType = damageType;
            HasTargetPosition = hasTargetPosition;
        }
    }

    /// <summary>
    /// Reconstructs visible shot, hit, death and production-ready cues without
    /// changing or subscribing to the deterministic simulation. The contract
    /// intentionally favours missed effects over false information: ambiguous
    /// disappearances are treated as fog/despawn and stay silent.
    /// </summary>
    public sealed class VisibleCombatFrameDiffer
    {
        private readonly Dictionary<EntityId, VisibleCombatSample> _previous =
            new Dictionary<EntityId, VisibleCombatSample>(256);
        private readonly Dictionary<EntityId, VisibleCombatSample> _current =
            new Dictionary<EntityId, VisibleCombatSample>(256);
        private readonly Dictionary<EntityId, ShotEvidence> _shotsByTarget =
            new Dictionary<EntityId, ShotEvidence>(64);

        private bool _hasBaseline;
        private uint _lastTick;

        /// <summary>The simulation tick most recently accepted by the differ.</summary>
        public uint LastObservedTick => _lastTick;

        /// <summary>Clears all history, for match restart or viewer-team changes.</summary>
        public void Reset(int capacity = 0)
        {
            _previous.Clear();
            _current.Clear();
            _shotsByTarget.Clear();
            _hasBaseline = false;
            _lastTick = 0;

            // Dictionary capacity is only a performance hint. EnsureCapacity
            // is available in the Unity 6/.NET Standard profile used here and
            // avoids a mid-battle growth when the match capacity is known.
            if (capacity > 0)
            {
                _previous.EnsureCapacity(capacity);
                _current.EnsureCapacity(capacity);
                _shotsByTarget.EnsureCapacity(Math.Min(capacity, 256));
            }
        }

        /// <summary>
        /// Compares one committed visible frame with the preceding tick. Calls
        /// for the same render tick are ignored, which prevents duplicate SFX
        /// when several Unity frames render one 10-Hz simulation state.
        /// </summary>
        public void Observe(
            uint tick,
            byte viewerTeam,
            IReadOnlyList<VisibleCombatSample> visibleSamples,
            List<CombatFeedbackEvent> output)
        {
            if (visibleSamples == null) throw new ArgumentNullException(nameof(visibleSamples));
            if (output == null) throw new ArgumentNullException(nameof(output));

            if (_hasBaseline && tick == _lastTick) return;
            if (_hasBaseline && tick < _lastTick)
            {
                // A restarted/restored match is a new observation domain. Its
                // first frame becomes a baseline instead of producing a wall
                // of false deaths and ready notifications.
                Reset(visibleSamples.Count);
            }

            _current.Clear();
            for (int i = 0; i < visibleSamples.Count; i++)
            {
                VisibleCombatSample sample = visibleSamples[i];
                if (!sample.Id.IsValid) continue;
                _current[sample.Id] = sample;
            }

            if (!_hasBaseline)
            {
                ReplaceBaseline(tick);
                return;
            }

            uint tickDelta = tick - _lastTick;
            _shotsByTarget.Clear();

            // Shots are derived first so later hit/death events can inherit
            // the visible shooter's damage type without dereferencing a target
            // that may already have disappeared in this same tick.
            foreach (KeyValuePair<EntityId, VisibleCombatSample> pair in _current)
            {
                VisibleCombatSample sample = pair.Value;
                if (!_previous.TryGetValue(pair.Key, out VisibleCombatSample before))
                {
                    if (sample.PlayerId == viewerTeam
                        && sample.Role != UnitRole.Unit
                        && !SimDefinitions.IsBuildingRole(sample.Role))
                    {
                        output.Add(UnitReady(sample));
                    }
                    continue;
                }

                // A construction-site promotion changes the meaning of the
                // same id. Treat it as a fresh baseline for that row; mixing
                // site health/cooldown into the finished role would fabricate
                // events.
                if (before.Role != sample.Role) continue;

                if (sample.WeaponCooldownTicks > before.WeaponCooldownTicks)
                {
                    EntityId targetId = sample.AttackTarget.IsValid
                        ? sample.AttackTarget
                        : before.AttackTarget;
                    if (!targetId.IsValid) continue;
                    bool hasTargetPosition = TryVisiblePosition(targetId, out Vector3 targetPosition);

                    output.Add(new CombatFeedbackEvent(
                        CombatFeedbackKind.Shot,
                        sample.Id,
                        targetId,
                        sample.Position,
                        targetPosition,
                        sample.Role,
                        ResolveRole(targetId),
                        sample.DamageType,
                        hasTargetPosition));

                    if (targetId.IsValid)
                    {
                        AddShotEvidence(targetId, sample);
                    }
                }
            }

            foreach (KeyValuePair<EntityId, VisibleCombatSample> pair in _current)
            {
                VisibleCombatSample sample = pair.Value;
                if (!_previous.TryGetValue(pair.Key, out VisibleCombatSample before)) continue;
                if (before.Role != sample.Role || sample.CurrentHealth >= before.CurrentHealth) continue;

                ShotEvidence evidence = GetShotEvidence(sample.Id);
                output.Add(new CombatFeedbackEvent(
                    CombatFeedbackKind.Hit,
                    evidence.SourceId,
                    sample.Id,
                    evidence.SourcePosition,
                    sample.Position,
                    evidence.SourceRole,
                    sample.Role,
                    evidence.Count > 0 ? evidence.DamageType : DamageType.Kinetic,
                    hasTargetPosition: true));
            }

            foreach (KeyValuePair<EntityId, VisibleCombatSample> pair in _previous)
            {
                if (_current.ContainsKey(pair.Key)) continue;

                VisibleCombatSample vanished = pair.Value;
                ShotEvidence evidence = GetShotEvidence(vanished.Id);
                bool ownMobileDeath = vanished.PlayerId == viewerTeam
                                      && vanished.Role != UnitRole.Unit
                                      && !SimDefinitions.IsBuildingRole(vanished.Role);
                bool correlatedDeath = tickDelta == 1
                                       && evidence.Count == 1
                                       && (vanished.PlayerId == viewerTeam
                                           || evidence.SourcePlayerId == viewerTeam);
                if (!ownMobileDeath && !correlatedDeath) continue;

                output.Add(new CombatFeedbackEvent(
                    CombatFeedbackKind.Death,
                    evidence.SourceId,
                    vanished.Id,
                    evidence.SourcePosition,
                    vanished.Position,
                    evidence.SourceRole,
                    vanished.Role,
                    evidence.Count > 0 ? evidence.DamageType : DamageType.Kinetic,
                    hasTargetPosition: true));
            }

            ReplaceBaseline(tick);
        }

        private void ReplaceBaseline(uint tick)
        {
            _previous.Clear();
            foreach (KeyValuePair<EntityId, VisibleCombatSample> pair in _current)
            {
                _previous.Add(pair.Key, pair.Value);
            }
            _lastTick = tick;
            _hasBaseline = true;
        }

        private bool TryVisiblePosition(EntityId id, out Vector3 position)
        {
            if (id.IsValid && _current.TryGetValue(id, out VisibleCombatSample current))
            {
                position = current.Position;
                return true;
            }
            if (id.IsValid && _previous.TryGetValue(id, out VisibleCombatSample previous))
            {
                position = previous.Position;
                return true;
            }
            position = default;
            return false;
        }

        private UnitRole ResolveRole(EntityId id)
        {
            if (id.IsValid && _current.TryGetValue(id, out VisibleCombatSample current)) return current.Role;
            if (id.IsValid && _previous.TryGetValue(id, out VisibleCombatSample previous)) return previous.Role;
            return UnitRole.Unit;
        }

        private void AddShotEvidence(EntityId targetId, in VisibleCombatSample shooter)
        {
            if (!_shotsByTarget.TryGetValue(targetId, out ShotEvidence evidence))
            {
                evidence = new ShotEvidence
                {
                    SourceId = shooter.Id,
                    SourcePlayerId = shooter.PlayerId,
                    SourcePosition = shooter.Position,
                    SourceRole = shooter.Role,
                    DamageType = shooter.DamageType,
                };
            }
            evidence.Count++;
            _shotsByTarget[targetId] = evidence;
        }

        private ShotEvidence GetShotEvidence(EntityId targetId)
        {
            if (targetId.IsValid && _shotsByTarget.TryGetValue(targetId, out ShotEvidence evidence))
            {
                return evidence;
            }
            return new ShotEvidence { SourceId = EntityId.Invalid };
        }

        private static CombatFeedbackEvent UnitReady(in VisibleCombatSample sample)
        {
            return new CombatFeedbackEvent(
                CombatFeedbackKind.UnitReady,
                sample.Id,
                EntityId.Invalid,
                sample.Position,
                default,
                sample.Role,
                UnitRole.Unit,
                sample.DamageType,
                hasTargetPosition: false);
        }

        private struct ShotEvidence
        {
            public int Count;
            public EntityId SourceId;
            public byte SourcePlayerId;
            public Vector3 SourcePosition;
            public UnitRole SourceRole;
            public DamageType DamageType;
        }
    }
}

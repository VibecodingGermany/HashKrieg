using System;
using Nova.Networking;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.State;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// The match configuration (sprint 12, strand A6): turns the hard-wired
    /// constants of the canonical match — local slot 0, AI slot 1, input
    /// delay 1, factions fixed in code — into data. The local loopback match
    /// keeps every current default; a network match overrides slot, delay
    /// and transport from the relay's offer.
    /// <para>
    /// Faction choice in the UI is deliberately NOT this sprint (sprint 13):
    /// the defaults stay Alliance/Legion. What A6 delivers is the structural
    /// freedom — the input layer keeps its Alliance definition ids either
    /// way.
    /// </para>
    /// </summary>
    public sealed class MatchConfig
    {
        /// <summary>Default network lockstep input delay (300 ms at 10 Hz, sprint A2a; the RTS corridor is 2–6).</summary>
        public const uint NetworkInputDelayTicks = 3;

        public ulong Seed = MatchBootstrap.CanonicalSeed;
        public ushort MapWidth = 128;
        public ushort MapHeight = 128;
        public int EntityCapacity = 1024;
        public long StartingCredits = EconomySystem.CanonicalMatchStartingCreditsAE;

        /// <summary>The local human slot; the relay assigns it in a network match.</summary>
        public byte LocalSlot = 0;

        /// <summary>Active slots of the match (MS-1: exactly two, ascending and unique).</summary>
        public byte[] ActiveSlots = { 0, 1 };

        /// <summary>Faction per slot (index = slot); default: Alliance vs Legion.</summary>
        public FactionId[] FactionPerSlot =
        {
            FactionId.Alliance, FactionId.Legion,
            FactionId.Alliance, FactionId.Alliance,
            FactionId.Alliance, FactionId.Alliance,
            FactionId.Alliance, FactionId.Alliance,
        };

        /// <summary>Slots driven by the skirmish AI (MS-1: at most one). Empty in a two-human match.</summary>
        public byte[] AiSlots = { 1 };

        /// <summary>Lockstep input delay; 1 locally, <see cref="NetworkInputDelayTicks"/> over the relay. Part of the match fingerprint.</summary>
        public uint InputDelayTicks = 1;

        /// <summary>
        /// Relay client used as the session transport. Null means the local
        /// loopback path. The reference is intentionally retained when the
        /// config is cloned; all mutable value arrays are copied.
        /// </summary>
        public RelayMatchClient Transport;

        /// <summary>Relay endpoint; required when <see cref="Transport"/> is set.</summary>
        public string RelayHost;
        public int RelayPort = 47777;

        /// <summary>Shared match code of the round; never logged, never persisted.</summary>
        public ulong MatchToken;

        public bool IsNetworkMatch => Transport != null;

        /// <summary>
        /// Validates the complete A6 contract and returns an ownership-safe
        /// clone. Callers may mutate their source arrays afterwards without
        /// changing a running match.
        /// </summary>
        public MatchConfig ValidateAndClone()
        {
            if (MapWidth == 0) throw new ArgumentOutOfRangeException(nameof(MapWidth));
            if (MapHeight == 0) throw new ArgumentOutOfRangeException(nameof(MapHeight));
            if (EntityCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(EntityCapacity));
            if (StartingCredits < 0) throw new ArgumentOutOfRangeException(nameof(StartingCredits));
            if (!RelayProtocol.IsSupportedInputDelay(InputDelayTicks))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(InputDelayTicks),
                    $"Input delay must be between {RelayProtocol.MinInputDelayTicks} and {RelayProtocol.MaxInputDelayTicks} ticks.");
            }
            if (ActiveSlots == null || ActiveSlots.Length != 2)
            {
                throw new ArgumentException("The MS-1 match requires exactly two active slots.", nameof(ActiveSlots));
            }

            bool localIsActive = false;
            byte previous = 0;
            for (int i = 0; i < ActiveSlots.Length; i++)
            {
                byte slot = ActiveSlots[i];
                if (slot >= CommandLimits.ReservedPlayerSlots)
                {
                    throw new ArgumentOutOfRangeException(nameof(ActiveSlots), $"Slot {slot} is outside the reserved range.");
                }
                if (i > 0 && slot <= previous)
                {
                    throw new ArgumentException("Active slots must be strictly ascending and unique.", nameof(ActiveSlots));
                }
                previous = slot;
                localIsActive |= slot == LocalSlot;
            }
            if (!localIsActive)
            {
                throw new ArgumentException("LocalSlot must be one of ActiveSlots.", nameof(LocalSlot));
            }
            if (ActiveSlots[0] != 0 || ActiveSlots[1] != 1)
            {
                throw new ArgumentException("The canonical MS-1 match owns global slots 0 and 1.", nameof(ActiveSlots));
            }

            if (FactionPerSlot == null || FactionPerSlot.Length != CommandLimits.ReservedPlayerSlots)
            {
                throw new ArgumentException(
                    $"FactionPerSlot must contain all {CommandLimits.ReservedPlayerSlots} reserved slots.",
                    nameof(FactionPerSlot));
            }
            for (int i = 0; i < FactionPerSlot.Length; i++)
            {
                if (FactionPerSlot[i] != FactionId.Alliance && FactionPerSlot[i] != FactionId.Legion)
                {
                    throw new ArgumentException($"FactionPerSlot[{i}] is undefined.", nameof(FactionPerSlot));
                }
            }

            if (AiSlots == null) throw new ArgumentNullException(nameof(AiSlots));
            if (AiSlots.Length > 1)
            {
                throw new ArgumentException("The MS-1 runner supports at most one AI slot.", nameof(AiSlots));
            }
            for (int i = 0; i < AiSlots.Length; i++)
            {
                byte aiSlot = AiSlots[i];
                if (aiSlot == LocalSlot)
                {
                    throw new ArgumentException("The local human slot cannot also be AI-controlled.", nameof(AiSlots));
                }
                bool active = false;
                for (int a = 0; a < ActiveSlots.Length; a++) active |= ActiveSlots[a] == aiSlot;
                if (!active) throw new ArgumentException("Every AI slot must be active.", nameof(AiSlots));
            }

            if (Transport != null)
            {
                if (string.IsNullOrWhiteSpace(RelayHost))
                {
                    throw new ArgumentException("A network match requires RelayHost.", nameof(RelayHost));
                }
                if (RelayPort < 1 || RelayPort > 65535)
                {
                    throw new ArgumentOutOfRangeException(nameof(RelayPort));
                }
                if (MatchToken == 0)
                {
                    throw new ArgumentException("A network match requires a non-zero match token.", nameof(MatchToken));
                }
                if (AiSlots.Length != 0)
                {
                    throw new ArgumentException("The two-human relay match cannot contain an AI slot.", nameof(AiSlots));
                }
            }
            else if (!string.IsNullOrEmpty(RelayHost))
            {
                throw new ArgumentException("RelayHost without a Transport would silently bypass lockstep.", nameof(Transport));
            }

            return new MatchConfig
            {
                Seed = Seed,
                MapWidth = MapWidth,
                MapHeight = MapHeight,
                EntityCapacity = EntityCapacity,
                StartingCredits = StartingCredits,
                LocalSlot = LocalSlot,
                ActiveSlots = (byte[])ActiveSlots.Clone(),
                FactionPerSlot = (FactionId[])FactionPerSlot.Clone(),
                AiSlots = (byte[])AiSlots.Clone(),
                InputDelayTicks = InputDelayTicks,
                Transport = Transport,
                RelayHost = RelayHost,
                RelayPort = RelayPort,
                MatchToken = MatchToken,
            };
        }

        /// <summary>Returns a validated clone with the relay's authoritative offer applied.</summary>
        public MatchConfig WithOffer(RelayMatchClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (!client.HasOffer) throw new InvalidOperationException("The relay client has no offer yet.");
            MatchConfig clone = ValidateAndClone();
            clone.Seed = client.Seed;
            clone.LocalSlot = client.AssignedSlot;
            clone.ActiveSlots = (byte[])client.ActiveSlots.Clone();
            clone.InputDelayTicks = client.InputDelayTicks;
            clone.Transport = client;
            return clone.ValidateAndClone();
        }

        /// <summary>The canonical local match against the skirmish AI — exactly today's behaviour.</summary>
        public static MatchConfig LocalVsAi(ulong seed, ushort mapWidth, ushort mapHeight, int entityCapacity, long startingCredits)
        {
            return new MatchConfig
            {
                Seed = seed,
                MapWidth = mapWidth,
                MapHeight = mapHeight,
                EntityCapacity = entityCapacity,
                StartingCredits = startingCredits,
            };
        }

        /// <summary>A two-human relay match; seed, slot and delay arrive with the server's offer.</summary>
        public static MatchConfig NetworkVsHuman(string host, int port, ulong matchToken,
            RelayMatchClient transport = null)
        {
            return new MatchConfig
            {
                Transport = transport ?? new RelayMatchClient(),
                RelayHost = host,
                RelayPort = port,
                MatchToken = matchToken,
                AiSlots = new byte[0],
                InputDelayTicks = NetworkInputDelayTicks,
            };
        }
    }
}

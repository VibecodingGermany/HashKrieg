using System;
using System.Collections.Generic;
using Nova.Core;
using Nova.Simulation.Commands;

namespace Nova.Simulation.Replays
{
    public readonly struct ReplayEntry
    {
        public ulong TickIndex { get; }
        public CommandEnvelope Command { get; }

        public ReplayEntry(ulong tickIndex, in CommandEnvelope command)
        {
            TickIndex = tickIndex;
            Command = command;
        }
    }

    /// <summary>
    /// Stores deterministic command history per tick for match replay playback.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public sealed class ReplayBuffer
    {
        private readonly List<ReplayEntry> _entries = new List<ReplayEntry>();

        public IReadOnlyList<ReplayEntry> Entries => _entries;
        public int TotalCommands => _entries.Count;

        public void RecordCommand(ulong tickIndex, in CommandEnvelope command)
        {
            _entries.Add(new ReplayEntry(tickIndex, in command));
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}

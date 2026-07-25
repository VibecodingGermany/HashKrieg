using Nova.Core;

namespace Nova.Simulation
{
    /// <summary>
    /// Sink interface for submitting command envelopes into the Lockstep simulation loop.
    /// Used by human UI input, peer networking adapters, and AI controllers.
    /// </summary>
    public interface ICommandSink
    {
        /// <summary>
        /// Submits a command envelope to be scheduled for simulation execution.
        /// Returns true if accepted by the sink.
        /// </summary>
        bool SubmitCommand(in CommandEnvelope envelope);
    }
}

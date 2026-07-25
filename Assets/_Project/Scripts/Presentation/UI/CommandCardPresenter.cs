using System;

namespace Nova.Presentation.UI
{
    [Flags]
    public enum CommandButtonType
    {
        None = 0,
        Move = 1 << 0,
        Stop = 1 << 1,
        Attack = 1 << 2,
        Build = 1 << 3
    }

    /// <summary>
    /// Presenter mapping unit selection states to active command card HUD buttons.
    /// </summary>
    public sealed class CommandCardPresenter
    {
        public CommandButtonType GetAvailableCommands(int selectedCount)
        {
            if (selectedCount <= 0) return CommandButtonType.None;

            // Combat units support Move, Stop, and Attack commands
            return CommandButtonType.Move | CommandButtonType.Stop | CommandButtonType.Attack;
        }
    }
}

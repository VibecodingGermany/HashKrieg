using System;
using UnityEngine;

namespace Nova.Gameplay.Match
{
    /// <summary>
    /// The running build's commit (sprint 14, D-094): read lazily, once, from
    /// the Resources stamp that <c>BuildCommitStamp</c> (Nova.Editor) writes
    /// before every player build. In the Editor — and in any build whose
    /// stamp is missing or empty — the commit is <see cref="EditorCommit"/>.
    /// The lobby (D-092) sends this value with create/join so the server can
    /// reject pairs whose builds would fail the relay's definitions and
    /// fingerprint checks anyway. Never throws: a missing stamp is the normal
    /// Editor situation, not an error.
    /// </summary>
    public static class BuildInfo
    {
        /// <summary>Commit reported when no build stamp exists (Editor play mode).</summary>
        public const string EditorCommit = "dev-editor";

        private static string _commit;

        /// <summary>The stamped short commit (with "-dirty" suffix when the worktree was dirty), or <see cref="EditorCommit"/>.</summary>
        public static string Commit
        {
            get
            {
                if (_commit == null)
                {
                    _commit = Load();
                }
                return _commit;
            }
        }

        private static string Load()
        {
            try
            {
                TextAsset stamp = Resources.Load<TextAsset>("NovaBuildCommit");
                string text = stamp != null ? stamp.text : null;
                return string.IsNullOrWhiteSpace(text) ? EditorCommit : text.Trim();
            }
            catch (Exception)
            {
                return EditorCommit;
            }
        }
    }
}

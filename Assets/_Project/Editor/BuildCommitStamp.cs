using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace Nova.Editor
{
    /// <summary>
    /// Stamps the build commit into
    /// Assets/_Project/Resources/NovaBuildCommit.txt before every player build
    /// (sprint 14, D-094), so <c>BuildInfo.Commit</c> can report it at runtime
    /// and the lobby (D-092) can reject mismatched pairs before the relay's
    /// fingerprint check has to. The value mirrors the packaging scripts
    /// (tools/packaging/build-mac.sh): a dirty worktree gets the "-dirty"
    /// suffix. The stamp file is git-ignored — a build artifact, not content.
    /// <para>
    /// Git being unreachable is NOT a build failure: the stamp becomes
    /// "unknown", a warning is logged and the build continues. "unknown"
    /// simply never matches a real commit in the lobby's build_mismatch
    /// check, which is the honest outcome for an unidentifiable build.
    /// </para>
    /// </summary>
    public sealed class BuildCommitStamp : IPreprocessBuildWithReport
    {
        private const string StampAssetPath = "Assets/_Project/Resources/NovaBuildCommit.txt";
        private const string FallbackCommit = "unknown";
        private const int GitTimeoutMilliseconds = 10000;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string commit = ReadCommit();
            try
            {
                string directory = Path.GetDirectoryName(StampAssetPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(StampAssetPath, commit + "\n");
                // Without the import the file would not enter this build's
                // Resources — it was just created, so the asset database does
                // not know it yet.
                AssetDatabase.ImportAsset(StampAssetPath);
                Debug.Log($"[BuildCommitStamp] Stamped build commit '{commit}' into {StampAssetPath}.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[BuildCommitStamp] Could not write {StampAssetPath}: {exception.Message} — " +
                    "the build continues and reports its previous stamp (or 'dev-editor' from the Editor).");
            }
        }

        private static string ReadCommit()
        {
            // Application.dataPath is "<project>/Assets"; the project root IS
            // the repository root in this repo.
            string repoRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (repoRoot == null
                || !TryRunGit(repoRoot, "rev-parse --short HEAD", out string commit)
                || commit.Length == 0)
            {
                Debug.LogWarning("[BuildCommitStamp] git is not reachable — stamping 'unknown'.");
                return FallbackCommit;
            }
            if (TryRunGit(repoRoot, "status --porcelain", out string status) && status.Length > 0)
            {
                commit += "-dirty";
            }
            return commit;
        }

        private static bool TryRunGit(string workingDirectory, string arguments, out string output)
        {
            output = string.Empty;
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (Process process = Process.Start(start))
                {
                    if (process == null)
                    {
                        return false;
                    }
                    // Read stdout BEFORE waiting: a full pipe buffer would
                    // otherwise deadlock WaitForExit (git status --porcelain
                    // can grow large in a dirty worktree).
                    output = process.StandardOutput.ReadToEnd().Trim();
                    if (!process.WaitForExit(GitTimeoutMilliseconds))
                    {
                        try { process.Kill(); } catch (Exception) { /* best effort */ }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

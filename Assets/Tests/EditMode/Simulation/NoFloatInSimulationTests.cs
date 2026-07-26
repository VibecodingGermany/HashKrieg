using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Determinism source guard (EditMode lane). Sprint hard rule 3 and
    /// docs/tech/SimulationCore.md section 9: the authoritative simulation is
    /// Q16.16 fixed point, so no IEEE-754 arithmetic may enter
    /// Assets/_Project/Scripts/Simulation/** or Core/**. A single float
    /// multiply that rounds differently on two CPUs desynchronises lockstep,
    /// and no unit test catches it — the sources have to be checked directly.
    /// <para>
    /// The scan strips comments and string literals first, so documentation
    /// that merely mentions floats never trips it.
    /// </para>
    /// <para>
    /// THE WHITELIST IS A RATCHET, NOT AN EXEMPTION LIST. Every entry must
    /// still contain a hit (<see cref="Whitelist_ContainsNoStaleEntries"/>), so
    /// cleaning a relic forces the entry out and the guard tightens by itself.
    /// Adding an entry is a deliberate, reviewable act.
    /// </para>
    /// Mirror of the .NET lane NoFloatInSimulationTests.
    /// </summary>
    [TestFixture]
    public sealed class NoFloatInSimulationTests
    {
        /// <summary>
        /// Scanned roots, relative to Assets/_Project/Scripts.
        /// </summary>
        private static readonly string[] ScannedRoots = { "Core", "Simulation" };

        /// <summary>
        /// Repo-relative paths (forward slashes, below Assets/_Project/Scripts)
        /// allowed to contain IEEE-754 tokens, each with the reason it exists.
        /// Nothing else in Core/** or Simulation/** may name float, double,
        /// decimal or Mathf in code.
        /// </summary>
        private static readonly Dictionary<string, string> FloatWhitelist = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // --- the fixed-point boundary itself ---
            ["Core/SimFixed.cs"] = "defines FromFloat/ToFloat — the boundary conversions every other file is banned from using",
            ["Core/SimMath.cs"] = "legacy float transcendental adapters, caller-less on the canonical path (SimTrig replaced them); deleted when the last prototype relic goes",
            ["Core/SimRandom.cs"] = "ISimRandom.NextFloat presentation helper; the authoritative paths use the integer draws",
            ["Core/ISimRandom.cs"] = "declares NextFloat (see Core/SimRandom.cs)",
            ["Core/SimClock.cs"] = "TickDeltaSeconds is the engine-facing frame constant, not a simulation value",

            // --- documented pre-G1 relics, none on the canonical tick path ---
            ["Simulation/Definitions/UnitDefinition.cs"] = "prototype definition record superseded by SimDefinitions/SimUnitDefinition (Q16.16)",
            ["Simulation/Definitions/WeaponDefinition.cs"] = "prototype definition record superseded by the Q16.16 weapon table",
            ["Simulation/Definitions/CommanderAbilityDefinition.cs"] = "prototype definition record for the not-yet-ported commander domain",
            ["Simulation/Commanders/CommanderSystem.cs"] = "commander domain is unported float scaffolding; not registered in the canonical tick order",
            ["Simulation/Factions/BiomassGrid.cs"] = "Evolved-faction prototype grid; not registered in the canonical tick order",
            ["Simulation/Factions/EvolvedFactionSystem.cs"] = "Evolved-faction prototype system; not registered in the canonical tick order",
        };

        /// <summary>
        /// IEEE-754 and engine-math tokens that must not appear in simulation
        /// code. <c>ToFloat()</c> is included on purpose: it is the other half
        /// of the boundary — a Q16.16 value converted to float and then used in
        /// an expression is exactly the platform-divergent arithmetic the
        /// fixed-point core exists to prevent.
        /// </summary>
        private static readonly Regex FloatTokens = new Regex(
            @"\b(float|double|decimal|Mathf|Single)\b|\.\s*ToFloat\s*\(", RegexOptions.Compiled);

        /// <summary>
        /// Zero-tolerance tokens: no whitelist at all outside the declaring
        /// file. A FromFloat CALL is the exact defect hard rule 3 forbids, and
        /// UnityEngine must not be reachable from rank 0/1 at all.
        /// </summary>
        private static readonly Regex ForbiddenEverywhere = new Regex(
            @"SimFixed\s*\.\s*FromFloat|(?<!\.)\bUnityEngine\b", RegexOptions.Compiled);

        /// <summary>The only file allowed to name SimFixed.FromFloat (it declares it).</summary>
        private const string FromFloatDeclarationFile = "Core/SimFixed.cs";

        // ----------------------------------------------------------------
        // Tests
        // ----------------------------------------------------------------

        [Test]
        public void NoFloatTokensOutsideTheDocumentedWhitelist()
        {
            var offenders = new List<string>();
            foreach (ScannedFile file in EnumerateSources())
            {
                if (FloatWhitelist.ContainsKey(file.RelativePath)) continue;

                foreach (string hit in Matches(file, FloatTokens))
                {
                    offenders.Add(hit);
                }
            }

            Assert.That(offenders, Is.Empty,
                "IEEE-754 arithmetic in the authoritative simulation breaks cross-platform lockstep " +
                "(sprint hard rule 3). Use SimFixed/SimTrig instead. If a file genuinely belongs at " +
                "the presentation boundary, move it out of Core/Simulation — do not grow the " +
                "whitelist without an explicit review.\n" + string.Join("\n", offenders));
        }

        [Test]
        public void NoFromFloatCallsAndNoEngineReferences()
        {
            var offenders = new List<string>();
            foreach (ScannedFile file in EnumerateSources())
            {
                if (file.RelativePath == FromFloatDeclarationFile) continue;

                foreach (string hit in Matches(file, ForbiddenEverywhere))
                {
                    offenders.Add(hit);
                }
            }

            Assert.That(offenders, Is.Empty,
                "SimFixed.FromFloat must never be called inside Core/Simulation, and rank 0/1 " +
                "assemblies must not reference UnityEngine at all.\n" + string.Join("\n", offenders));
        }

        [Test]
        public void Whitelist_ContainsNoStaleEntries()
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            var stillDirty = new HashSet<string>(StringComparer.Ordinal);

            foreach (ScannedFile file in EnumerateSources())
            {
                known.Add(file.RelativePath);
                if (FloatTokens.IsMatch(file.StrippedText)) stillDirty.Add(file.RelativePath);
            }

            var stale = new List<string>();
            foreach (KeyValuePair<string, string> entry in FloatWhitelist)
            {
                if (!known.Contains(entry.Key))
                {
                    stale.Add($"{entry.Key}: file no longer exists — drop the whitelist entry");
                }
                else if (!stillDirty.Contains(entry.Key))
                {
                    stale.Add($"{entry.Key}: no float token left — drop the whitelist entry ({entry.Value})");
                }
            }

            Assert.That(stale, Is.Empty,
                "The whitelist is a ratchet: an entry that is no longer needed must go, so the " +
                "guard tightens as the prototype relics are removed.\n" + string.Join("\n", stale));
        }

        [Test]
        public void ScanActuallyReachesTheSimulationSources()
        {
            // A path-resolution bug would make every test above vacuously pass.
            var seen = new List<string>();
            foreach (ScannedFile file in EnumerateSources()) seen.Add(file.RelativePath);

            Assert.That(seen.Count, Is.GreaterThan(50), "expected the full Core+Simulation source set");
            Assert.That(seen, Contains.Item("Core/SimFixed.cs"));
            Assert.That(seen, Contains.Item("Simulation/SimulationKernel.cs"));
            Assert.That(seen, Contains.Item("Simulation/Economy/EconomySystem.cs"));
        }

        // ----------------------------------------------------------------
        // Scanning
        // ----------------------------------------------------------------

        private readonly struct ScannedFile
        {
            public ScannedFile(string relativePath, string strippedText)
            {
                RelativePath = relativePath;
                StrippedText = strippedText;
            }

            /// <summary>Forward-slash path below Assets/_Project/Scripts.</summary>
            public string RelativePath { get; }

            /// <summary>Source with comments and string/char literals blanked out.</summary>
            public string StrippedText { get; }
        }

        private static IEnumerable<ScannedFile> EnumerateSources()
        {
            string scriptsRoot = ResolveScriptsRoot();
            foreach (string root in ScannedRoots)
            {
                string absoluteRoot = Path.Combine(scriptsRoot, root);
                Assert.That(Directory.Exists(absoluteRoot), Is.True, $"missing scanned root {absoluteRoot}");

                foreach (string path in Directory.GetFiles(absoluteRoot, "*.cs", SearchOption.AllDirectories))
                {
                    string relative = path.Substring(scriptsRoot.Length + 1).Replace('\\', '/');
                    yield return new ScannedFile(relative, StripCommentsAndLiterals(File.ReadAllText(path)));
                }
            }
        }

        private static IEnumerable<string> Matches(ScannedFile file, Regex pattern)
        {
            foreach (Match match in pattern.Matches(file.StrippedText))
            {
                int line = 1;
                for (int i = 0; i < match.Index; i++)
                {
                    if (file.StrippedText[i] == '\n') line++;
                }
                yield return $"  {file.RelativePath}:{line}: '{match.Value}'";
            }
        }

        /// <summary>
        /// THE ONE LANE-SPECIFIC METHOD. The .NET mirror walks up from the test
        /// binary; here Application.dataPath IS the Assets folder, so the scan
        /// is exact regardless of checkout path.
        /// </summary>
        private static string ResolveScriptsRoot()
        {
            string candidate = Path.Combine(Application.dataPath, "_Project", "Scripts");
            Assert.That(Directory.Exists(candidate), Is.True,
                $"could not locate {candidate} — Application.dataPath is {Application.dataPath}");
            return candidate;
        }

        /// <summary>
        /// Blanks out //, /* */ and string/char literals (verbatim and
        /// interpolated included) while preserving newlines, so line numbers in
        /// the failure message stay accurate.
        /// </summary>
        private static string StripCommentsAndLiterals(string source)
        {
            var output = new StringBuilder(source.Length);
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];

                if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    while (i < source.Length && source[i] != '\n') { output.Append(' '); i++; }
                    continue;
                }

                if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    output.Append("  ");
                    i += 2;
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                    {
                        output.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                    if (i < source.Length) { output.Append("  "); i += 2; }
                    continue;
                }

                if (c == '@' && i + 1 < source.Length && source[i + 1] == '"')
                {
                    output.Append("  ");
                    i += 2;
                    while (i < source.Length)
                    {
                        if (source[i] == '"')
                        {
                            if (i + 1 < source.Length && source[i + 1] == '"') { output.Append("  "); i += 2; continue; }
                            output.Append(' '); i++; break;
                        }
                        output.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    char quote = c;
                    output.Append(' ');
                    i++;
                    while (i < source.Length && source[i] != quote)
                    {
                        if (source[i] == '\\' && i + 1 < source.Length) { output.Append("  "); i += 2; continue; }
                        if (source[i] == '\n') break; // unterminated: bail out rather than eat the file
                        output.Append(' ');
                        i++;
                    }
                    if (i < source.Length && source[i] == quote) { output.Append(' '); i++; }
                    continue;
                }

                output.Append(c);
                i++;
            }
            return output.ToString();
        }
    }
}

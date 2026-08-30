using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Source-text guard for the canonical Glutrinne terrain mirror
    /// (21.7/#94/D-109, gap found in the sprint-21 adversarial re-read).
    /// The map exists twice by necessity: Gameplay/Match/GlutrinneTerrainMap.cs
    /// feeds the Unity host, Determinism10000Scenario.GlutrinneTerrain feeds
    /// the headless lane — the csproj boundary (Core/Simulation/Networking/AI
    /// only, a frozen boundary) forbids referencing the Gameplay assembly.
    /// The EditMode lane pins the Unity side against the shared FNV-1a
    /// checksum, but NO CI runs it (#110), so a one-sided edit of the Gameplay
    /// source used to leave every CI check green while host and guest
    /// computed different maps — a desync that would surface as "units walk
    /// elsewhere on the other screen" and that nobody would trace back to the
    /// map work.
    /// <para>
    /// This guard closes the hole from the lane that DOES run. It reads both
    /// copies as SOURCE TEXT — the NoFloatInSimulationTests /
    /// PresentationSourceBoundaryTests pattern, since Gameplay sources are not
    /// compiled in this assembly — and pins two layers:
    /// </para>
    /// <para>
    /// 1. The CONSTANTS as parsed values, compared against the compiled
    /// CanonicalTerrainMirror reference (which GlutrinneTerrainTests pins
    /// cell-exact against the scenario's runtime behaviour). Parsing the
    /// declarations tolerates any reformatting; a value edit or a rename of
    /// the shared names trips it.
    /// </para>
    /// <para>
    /// 2. The IsImpassable PREDICATE of both copies, comment-stripped and
    /// whitespace-normalised, compared token-for-token AGAINST EACH OTHER —
    /// deliberately not against a third literal in this file, so a red guard
    /// can only ever mean "the two copies disagree" and the remedy is always
    /// to reconcile the copies, never to edit the guard. Reformatting and
    /// docstring edits stay green; a formula edit on one side (<c>&lt;</c>
    /// vs <c>&lt;=</c> widens every gap by a cell without touching any
    /// constant) trips it.
    /// </para>
    /// <para>
    /// What this guard deliberately does NOT pin: the Apply stamping loops
    /// (the bodies legitimately differ — the Gameplay one carries a null
    /// check; the stamped content and count are pinned behaviourally by the
    /// cell-exact and epoch assertions in GlutrinneTerrainTests), all
    /// formatting, and all comments. It is a MOVED-WITHOUT-THE-OTHER
    /// detector, not a semantic proof: a consistent-but-wrong edit of every
    /// copy at once is what the EditMode checksum lane is for — run it
    /// locally, it is not in CI (#110).
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class GlutrinneTerrainSourceGuardTests
    {
        private const string GameplaySourceRelativePath =
            "Assets/_Project/Scripts/Gameplay/Match/GlutrinneTerrainMap.cs";

        private const string ScenarioSourceRelativePath =
            "tools/Nova.SimRunner/Determinism10000Scenario.cs";

        /// <summary>
        /// The remedy every failure in this fixture points at. The guard must
        /// never become the thing a developer "fixes".
        /// </summary>
        private const string MirrorRemedy =
            "The canonical Glutrinne terrain exists as hand-mirrored copies: " +
            "Assets/_Project/Scripts/Gameplay/Match/GlutrinneTerrainMap.cs (Unity host), " +
            "GlutrinneTerrain in tools/Nova.SimRunner/Determinism10000Scenario.cs (headless lane) and " +
            "CanonicalTerrainMirror in tools/Nova.SimRunner.Tests/GlutrinneTerrainTests.cs (pinned reference). " +
            "One of them moved without the others — left standing, host and guest compute DIFFERENT maps " +
            "and desync. Apply the change to EVERY copy (and keep the pinned checksums consistent); " +
            "never silence this guard by editing the guard. " +
            "The EditMode CanonicalMatchSetupTests pin the same content on the Unity host and are NOT in CI (#110) — run them locally.";

        /// <summary>The constant names shared by all three copies — part of the mirror contract.</summary>
        private static readonly (string Name, int Expected)[] PinnedConstants =
        {
            ("CentreX", CanonicalTerrainMirror.CentreX),
            ("CentreY", CanonicalTerrainMirror.CentreY),
            ("RingInnerRadius", CanonicalTerrainMirror.RingInnerRadius),
            ("RingOuterRadius", CanonicalTerrainMirror.RingOuterRadius),
            ("CornerGapMinRadius", CanonicalTerrainMirror.CornerGapMinRadius),
            ("ImpassableCellCount", CanonicalTerrainMirror.ImpassableCellCount),
        };

        private static readonly Regex ConstantDeclaration = new Regex(
            @"\bpublic\s+const\s+int\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>\d+)\s*;",
            RegexOptions.Compiled);

        private static readonly Regex PredicateSignature = new Regex(
            @"\bbool\s+IsImpassable\s*\(\s*int\s+x\s*,\s*int\s+y\s*\)",
            RegexOptions.Compiled);

        // ----------------------------------------------------------------
        // Tests
        // ----------------------------------------------------------------

        [Test]
        public void GameplaySource_TerrainConstants_MatchTheHeadlessMirror()
        {
            Dictionary<string, int> parsed = ParseConstants(ReadStripped(GameplaySourceRelativePath));

            foreach ((string name, int expected) in PinnedConstants)
            {
                Assert.That(parsed.TryGetValue(name, out int actual), Is.True,
                    $"constant {name} no longer parses from {GameplaySourceRelativePath} — renamed or removed. " +
                    "The name is part of the mirror contract: rename it in every copy or revert the rename. " + MirrorRemedy);
                Assert.That(actual, Is.EqualTo(expected),
                    $"constant {name} reads {actual} in the Gameplay source but the headless mirror computes with {expected}. " +
                    MirrorRemedy);
            }
        }

        [Test]
        public void GameplaySource_TerrainPredicate_MatchesTheHeadlessMirrorTokenForToken()
        {
            string gameplayPredicate = ExtractPredicateBody(ReadStripped(GameplaySourceRelativePath), GameplaySourceRelativePath);
            string scenarioPredicate = ExtractPredicateBody(ReadStripped(ScenarioSourceRelativePath), ScenarioSourceRelativePath);

            Assert.That(gameplayPredicate, Is.EqualTo(scenarioPredicate),
                "the IsImpassable predicate moved on one side only (comments and whitespace are ignored in this comparison). " +
                MirrorRemedy +
                $"\n  gameplay : {gameplayPredicate}\n  mirror   : {scenarioPredicate}");
        }

        [Test]
        public void SourceGuard_ActuallyReadsBothTerrainCopies()
        {
            // A path-resolution or extraction bug would make the two pins
            // above vacuously green — the guard has to prove it sees the real
            // files, just like the scan-reach tests of the cousin guards.
            string gameplay = ReadStripped(GameplaySourceRelativePath);
            string scenario = ReadStripped(ScenarioSourceRelativePath);

            Assert.That(gameplay, Does.Contain("GlutrinneTerrainMap"));
            Assert.That(scenario, Does.Contain("GlutrinneTerrain"));
            Assert.That(ParseConstants(gameplay), Has.Count.GreaterThanOrEqualTo(PinnedConstants.Length),
                "expected at least the six pinned terrain constants in the Gameplay source");
            Assert.That(ExtractPredicateBody(gameplay, GameplaySourceRelativePath), Is.Not.Empty);
            Assert.That(ExtractPredicateBody(scenario, ScenarioSourceRelativePath), Is.Not.Empty);
        }

        // ----------------------------------------------------------------
        // Reading and extraction
        // ----------------------------------------------------------------

        private static Dictionary<string, int> ParseConstants(string strippedSource)
        {
            var constants = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match match in ConstantDeclaration.Matches(strippedSource))
            {
                constants[match.Groups["name"].Value] = int.Parse(match.Groups["value"].Value);
            }
            return constants;
        }

        /// <summary>
        /// The body of <c>IsImpassable</c> with every whitespace character
        /// removed: reformatting and rewrapping stay invisible, token moves do
        /// not. Comments and literals are already blanked by the caller, so
        /// brace matching cannot trip on a literal.
        /// </summary>
        private static string ExtractPredicateBody(string strippedSource, string pathForMessage)
        {
            Match signature = PredicateSignature.Match(strippedSource);
            Assert.That(signature.Success, Is.True,
                $"no 'bool IsImpassable(int x, int y)' signature found in {pathForMessage} — " +
                "renamed? The mirror contract includes this name. " + MirrorRemedy);

            int open = strippedSource.IndexOf('{', signature.Index + signature.Length);
            Assert.That(open, Is.GreaterThanOrEqualTo(0), $"no predicate body after the IsImpassable signature in {pathForMessage}");

            int depth = 0;
            for (int i = open; i < strippedSource.Length; i++)
            {
                if (strippedSource[i] == '{') depth++;
                else if (strippedSource[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        string body = strippedSource.Substring(open + 1, i - open - 1);
                        var normalised = new StringBuilder(body.Length);
                        foreach (char c in body)
                        {
                            if (!char.IsWhiteSpace(c)) normalised.Append(c);
                        }
                        return normalised.ToString();
                    }
                }
            }
            Assert.Fail($"unbalanced braces in the IsImpassable body of {pathForMessage}");
            return null;
        }

        /// <summary>
        /// Reads one repo file with comments and literals blanked. The root is
        /// resolved from the test binary and must contain BOTH copies, so a
        /// partial checkout fails loudly instead of comparing nothing.
        /// </summary>
        private static string ReadStripped(string relativePath)
        {
            string root = ResolveRepoRoot();
            string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(path), Is.True, $"terrain copy missing: {path}");
            return StripCommentsAndLiterals(File.ReadAllText(path));
        }

        private static string ResolveRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                bool gameplayHere = File.Exists(Path.Combine(directory.FullName, GameplaySourceRelativePath));
                bool scenarioHere = File.Exists(Path.Combine(directory.FullName, ScenarioSourceRelativePath));
                if (gameplayHere && scenarioHere) return directory.FullName;
                directory = directory.Parent;
            }
            Assert.Fail($"could not locate both terrain copies above {AppContext.BaseDirectory}");
            return null;
        }

        /// <summary>
        /// Blanks out //, /* */ and string/char literals (verbatim and
        /// interpolated included) while preserving newlines — identical to the
        /// stripper of NoFloatInSimulationTests and
        /// PresentationSourceBoundaryTests.
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

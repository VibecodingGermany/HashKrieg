using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Production-source guard for the Sprint-12B presentation boundary. It
    /// replaces the unbuildable runtime A/B-hash experiment: code above the
    /// Simulation assembly may read snapshots, but may neither take a mutable
    /// UnitState ref nor consume the deterministic random stream.
    /// <para>
    /// Tests are intentionally outside the scan because simulation fixtures
    /// legitimately use GetUnitRef to arrange authoritative state. Comments
    /// and literals are blanked while newlines are retained, so diagnostics
    /// point at real code and stable line numbers.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class PresentationSourceBoundaryTests
    {
        private static readonly Regex MutableUnitAccess = new Regex(
            @"\bGetUnitRef\s*\(", RegexOptions.Compiled);

        private static readonly Regex RandomMemberAccess = new Regex(
            @"\.\s*Random\b", RegexOptions.Compiled);

        [Test]
        public void ProductionCodeOutsideSimulationCannotMutateUnitsOrReadSimulationRandom()
        {
            var offenders = new List<string>();
            foreach (ScannedFile file in EnumerateProductionSources())
            {
                foreach (Match match in MutableUnitAccess.Matches(file.StrippedText))
                {
                    offenders.Add(Format(file, match));
                }

                foreach (Match match in RandomMemberAccess.Matches(file.StrippedText))
                {
                    string qualifier = ImmediateQualifier(file.StrippedText, match.Index);
                    if (qualifier == "UnityEngine" || qualifier == "System") continue;
                    offenders.Add(Format(file, match));
                }
            }

            Assert.That(offenders, Is.Empty,
                "Production code outside Simulation/** must use snapshot/read-only surfaces. " +
                "GetUnitRef mutates authoritative state and a simulation Random member would " +
                "consume deterministic entropy from presentation code. UnityEngine.Random and " +
                "System.Random remain legal cosmetic/platform randomness.\n" +
                string.Join("\n", offenders));
        }

        [Test]
        public void ScanReachesTheCombatAndInputProductionSources()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ScannedFile file in EnumerateProductionSources()) seen.Add(file.RelativePath);

            Assert.That(seen.Count, Is.GreaterThan(40), "expected the non-Simulation production source tree");
            Assert.That(seen, Contains.Item("Gameplay/Match/UnitViewManager.cs"));
            Assert.That(seen, Contains.Item("Gameplay/CombatFeedback/VisibleCombatFrameDiffer.cs"));
            Assert.That(seen, Contains.Item("Presentation/UI/RtsDeviceInput.cs"));
        }

        private static IEnumerable<ScannedFile> EnumerateProductionSources()
        {
            string root = ResolveScriptsRoot();
            foreach (string path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string relative = path.Substring(root.Length + 1).Replace('\\', '/');
                if (relative.StartsWith("Simulation/", StringComparison.Ordinal)) continue;
                yield return new ScannedFile(relative, StripCommentsAndLiterals(File.ReadAllText(path)));
            }
        }

        private static string ResolveScriptsRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Assets", "_Project", "Scripts");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            Assert.Fail($"could not locate Assets/_Project/Scripts above {AppContext.BaseDirectory}");
            return null;
        }

        private static string Format(ScannedFile file, Match match)
        {
            int line = 1;
            for (int i = 0; i < match.Index; i++)
            {
                if (file.StrippedText[i] == '\n') line++;
            }
            return $"  {file.RelativePath}:{line}: '{match.Value}'";
        }

        private static string ImmediateQualifier(string source, int dotIndex)
        {
            int end = dotIndex - 1;
            while (end >= 0 && char.IsWhiteSpace(source[end])) end--;
            int start = end;
            while (start >= 0 && (char.IsLetterOrDigit(source[start]) || source[start] == '_')) start--;
            return end >= start + 1 ? source.Substring(start + 1, end - start) : string.Empty;
        }

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
                            if (i + 1 < source.Length && source[i + 1] == '"')
                            {
                                output.Append("  ");
                                i += 2;
                                continue;
                            }
                            output.Append(' ');
                            i++;
                            break;
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
                        if (source[i] == '\\' && i + 1 < source.Length)
                        {
                            output.Append("  ");
                            i += 2;
                            continue;
                        }
                        if (source[i] == '\n') break;
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

        private readonly struct ScannedFile
        {
            public string RelativePath { get; }
            public string StrippedText { get; }

            public ScannedFile(string relativePath, string strippedText)
            {
                RelativePath = relativePath;
                StrippedText = strippedText;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace V57.GameForge.Editor
{
    [Serializable]
    public class TddMechanicInfo
    {
        public string id;
        public string title;
        public string type;
    }

    [Serializable]
    public class TddInfo
    {
        public string slug;
        public string projectName;
        public string path;
        public List<TddMechanicInfo> mechanics = new();
    }

    /// <summary>
    /// Lightweight V57 TDD markdown parser.
    /// </summary>
    public static class TddParser
    {
        private static readonly Regex MechanicHeading =
            new(@"^##\s+Mechanic:\s*(.+)\s*$", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex TypeLine =
            new(@"^\s*-\s*type:\s*(\S+)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ProjectNameYaml =
            new(@"project_name:\s*[""']?([^\r\n""']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex GameTitleTable =
            new(@"\|\s*\*\*Game title\*\*\s*\|\s*([^|]+)\|", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TddH1Title =
            new(@"^#\s*TDD\s*[—–-]\s*(.+?)(?:\s*\(|$)", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> SkipMd = new(StringComparer.OrdinalIgnoreCase)
        {
            "readme.md",
            "changelog.md"
        };

        public static string TddsRoot =>
            Path.Combine(GameForgeBootstrap.ProjectRoot, "Docs", "tdds");

        /// <summary>
        /// Resolve canonical TDD markdown inside Docs/tdds/&lt;slug&gt;/ (not required to be TDD.md).
        /// </summary>
        public static string ResolveTddFilePath(string slug)
        {
            var dir = Path.Combine(TddsRoot, slug);
            if (!Directory.Exists(dir))
                throw new FileNotFoundException($"TDD folder not found: Docs/tdds/{slug}/");

            var mdFiles = Directory.GetFiles(dir, "*.md")
                .Select(Path.GetFileName)
                .Where(f => f != null && !SkipMd.Contains(f))
                .Cast<string>()
                .ToList();

            if (mdFiles.Count == 0)
                throw new FileNotFoundException($"No TDD markdown in Docs/tdds/{slug}/");

            if (mdFiles.Contains("TDD.md", StringComparer.OrdinalIgnoreCase))
                return Path.Combine(dir, "TDD.md");

            var slugMd = $"{slug}.md";
            var slugMatch = mdFiles.FirstOrDefault(f => string.Equals(f, slugMd, StringComparison.OrdinalIgnoreCase));
            if (slugMatch != null)
                return Path.Combine(dir, slugMatch);

            if (mdFiles.Count == 1)
                return Path.Combine(dir, mdFiles[0]);

            var tddPref = mdFiles.Where(f => f.StartsWith("TDD", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (tddPref.Count > 0)
                return Path.Combine(dir, tddPref[0]);

            mdFiles.Sort(StringComparer.OrdinalIgnoreCase);
            return Path.Combine(dir, mdFiles[0]);
        }

        public static List<TddInfo> ListTdds()
        {
            var root = TddsRoot;
            var list = new List<TddInfo>();
            if (!Directory.Exists(root)) return list;

            foreach (var dir in Directory.GetDirectories(root))
            {
                var slug = Path.GetFileName(dir);
                try
                {
                    list.Add(ReadTdd(slug));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameForge] Skipping Docs/tdds/{slug}: {ex.Message}");
                }
            }

            list.Sort((a, b) => string.Compare(a.slug, b.slug, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        public static TddInfo ReadTdd(string slug)
        {
            var tddPath = ResolveTddFilePath(slug);
            var text = File.ReadAllText(tddPath);
            var info = new TddInfo
            {
                slug = slug,
                path = tddPath,
                projectName = ExtractProjectName(text) ?? slug,
                mechanics = ParseMechanics(text)
            };
            return info;
        }

        public static string ExtractProjectName(string text)
        {
            var m = ProjectNameYaml.Match(text);
            if (m.Success) return m.Groups[1].Value.Trim();
            m = GameTitleTable.Match(text);
            if (m.Success) return m.Groups[1].Value.Trim();
            m = TddH1Title.Match(text);
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        public static List<TddMechanicInfo> ParseMechanics(string text)
        {
            var result = new List<TddMechanicInfo>();
            var matches = MechanicHeading.Matches(text);
            for (var i = 0; i < matches.Count; i++)
            {
                var title = matches[i].Groups[1].Value.Trim();
                var start = matches[i].Index + matches[i].Length;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                var body = text.Substring(start, end - start);
                var typeMatch = TypeLine.Match(body);
                var type = typeMatch.Success ? typeMatch.Groups[1].Value.Trim() : "feature";
                result.Add(new TddMechanicInfo
                {
                    id = ToMechanicId(title),
                    title = title,
                    type = type
                });
            }

            return result;
        }

        public static string ToMechanicId(string title)
        {
            var s = Regex.Replace(title.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
            return s.Trim('-');
        }

        public static string ImportTdd(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException(sourceFilePath);
            var text = File.ReadAllText(sourceFilePath);
            var name = ExtractProjectName(text) ?? Path.GetFileNameWithoutExtension(sourceFilePath);
            var slug = ToMechanicId(name);
            if (string.IsNullOrEmpty(slug)) slug = "tdd";
            var destDir = Path.Combine(TddsRoot, slug);
            var n = 2;
            while (Directory.Exists(destDir))
            {
                destDir = Path.Combine(TddsRoot, $"{slug}-{n}");
                n++;
            }

            Directory.CreateDirectory(destDir);
            var outName = string.Equals(Path.GetExtension(sourceFilePath), ".md", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(sourceFilePath)
                : "TDD.md";
            File.WriteAllText(Path.Combine(destDir, outName), text);
            return Path.GetFileName(destDir);
        }
    }
}

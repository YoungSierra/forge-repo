using System.Text;

namespace V57.RoslynIndex;

internal static class DriftReporter
{
    public static string Generate(string projectRoot, ProjectIndex index, IReadOnlyList<SpecTouchInfo> specs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# V57 Drift Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:o}");
        sb.AppendLine();
        sb.AppendLine("Compares spec `touches` paths vs filesystem vs `project-index.json`.");
        sb.AppendLine();
        var issues = 0;

        sb.AppendLine("## Spec touches — missing on disk");
        sb.AppendLine();
        foreach (var spec in specs)
        {
            var allTouches = spec.GetAllTouchPaths();
            var missing = allTouches.Where(t => !File.Exists(Path.Combine(projectRoot, t.Replace('/', Path.DirectorySeparatorChar)))).ToList();
            if (missing.Count == 0)
            {
                continue;
            }

            issues += missing.Count;
            sb.AppendLine($"### `{spec.SpecPath}` ({spec.SpecId ?? spec.ModuleName})");
            foreach (var path in missing)
            {
                sb.AppendLine($"- MISSING: `{path}`");
            }

            sb.AppendLine();
        }

        if (issues == 0)
        {
            sb.AppendLine("_No missing touch paths._");
            sb.AppendLine();
        }

        sb.AppendLine("## Specs without `specId` or `touches`");
        sb.AppendLine();
        var legacy = specs.Where(s => string.IsNullOrWhiteSpace(s.SpecId) || s.GetAllTouchPaths().Count == 0).ToList();
        if (legacy.Count == 0)
        {
            sb.AppendLine("_All specs have specId and touches._");
        }
        else
        {
            foreach (var spec in legacy)
            {
                sb.AppendLine($"- `{spec.SpecPath}` — specId: {(string.IsNullOrWhiteSpace(spec.SpecId) ? "missing" : spec.SpecId)}, touches: {spec.GetAllTouchPaths().Count}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Index modules without matching spec");
        sb.AppendLine();
        var specIds = specs.Select(s => s.ModuleName).Where(n => !string.IsNullOrWhiteSpace(n)).ToHashSet(StringComparer.Ordinal);
        foreach (var module in index.Modules.Where(m => m.Id != "_Unassigned" && !specIds.Contains(m.Id)))
        {
            sb.AppendLine($"- Module `{module.Id}` in index but no spec `name` match");
        }

        sb.AppendLine();
        sb.AppendLine("## Recommended actions");
        sb.AppendLine();
        sb.AppendLine("1. Add `specId` + `touches` to legacy specs (see `V57/specs/template/feature_spec_template.yaml`).");
        sb.AppendLine("2. Re-run `V57/tools/index-project.ps1` after fixes.");
        sb.AppendLine("3. Propose CONTEXT auto-block: `dotnet run ... --propose-context` (confirm-gated).");

        return sb.ToString();
    }
}

internal static class SpecTouchExtensions
{
    public static List<string> GetAllTouchPaths(this SpecTouchInfo spec) =>
        spec.TouchScripts
            .Concat(spec.TouchPrefabs)
            .Concat(spec.TouchScriptableObjects)
            .Concat(spec.TouchTests)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

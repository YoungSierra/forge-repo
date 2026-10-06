using System.Text;

namespace V57.RoslynIndex;

internal static class ContextPatchGenerator
{
    public static string Generate(ProjectIndex index)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Proposed CONTEXT.md patch — Auto-generated index");
        sb.AppendLine();
        sb.AppendLine("> **Confirm-gated:** apply only after user approves. Insert/replace block between markers in root `CONTEXT.md`.");
        sb.AppendLine();
        sb.AppendLine($"Generated: {index.GeneratedAt} via `v57-index --propose-context`");
        sb.AppendLine();
        sb.AppendLine("```markdown");
        sb.AppendLine("<!-- v57:auto-index:start — DO NOT EDIT MANUALLY -->");
        sb.AppendLine("## Auto-generated index");
        sb.AppendLine();
        sb.AppendLine($"_Last run: {index.GeneratedAt}_");
        sb.AppendLine();
        sb.AppendLine("| Module | Spec | Scripts | Key deps |");
        sb.AppendLine("|--------|------|---------|----------|");

        foreach (var module in index.Modules.Where(m => m.Id != "_Unassigned"))
        {
            var deps = module.DependsOn.Count > 0
                ? string.Join(", ", module.DependsOn.Take(5))
                : "—";
            if (module.DependsOn.Count > 5)
            {
                deps += ", …";
            }

            sb.AppendLine($"| {module.Id} | {module.SpecId ?? "—"} | {module.Scripts.Count} | {deps} |");
        }

        if (index.Modules.Any(m => m.Id == "_Unassigned"))
        {
            var unassigned = index.Modules.First(m => m.Id == "_Unassigned");
            sb.AppendLine();
            sb.AppendLine($"_Unassigned scripts ({unassigned.Scripts.Count}): link to specs via `touches` in YAML._");
        }

        sb.AppendLine("<!-- v57:auto-index:end -->");
        sb.AppendLine("```");
        return sb.ToString();
    }
}

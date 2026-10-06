using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace V57.RoslynIndex;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Main(string[] args)
    {
        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (!Directory.Exists(options.ProjectRoot))
        {
            Console.Error.WriteLine($"Project root not found: {options.ProjectRoot}");
            return 1;
        }

        var builder = new IndexBuilder(options.ProjectRoot);
        var index = builder.Build();

        var scriptsRoot = IndexBuilder.ResolveScriptsRoot(options.ProjectRoot);
        if (index.Modules.Count == 0 && Directory.Exists(scriptsRoot))
        {
            Console.Error.WriteLine(
                $"Error: index is empty but scripts folder exists at '{scriptsRoot}'. Check specs and script paths.");
            return 1;
        }

        var unityAssetsPath = Path.Combine(options.ProjectRoot, "v57-unity-assets.json");
        if (File.Exists(unityAssetsPath))
        {
            builder.MergeUnityAssets(index, unityAssetsPath);
        }

        var outputPath = options.OutputPath
            ?? Path.Combine(options.ProjectRoot, "project-index.json");

        var json = JsonSerializer.Serialize(index, JsonOptions);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        Console.WriteLine($"Wrote {outputPath} ({index.Modules.Count} modules, {index.Interfaces.Count} interfaces)");

        if (options.ProposeContext)
        {
            var patchPath = options.ContextPatchPath
                ?? Path.Combine(options.ProjectRoot, "V57", "docs", "reports", "context-auto-patch.md");
            Directory.CreateDirectory(Path.GetDirectoryName(patchPath)!);
            File.WriteAllText(patchPath, ContextPatchGenerator.Generate(index), Encoding.UTF8);
            Console.WriteLine($"Wrote proposed CONTEXT patch: {patchPath}");
        }

        if (options.DriftReport)
        {
            var drift = DriftReporter.Generate(options.ProjectRoot, index, builder.Specs);
            var driftPath = options.DriftReportPath
                ?? Path.Combine(options.ProjectRoot, "V57", "docs", "reports", "drift-report.md");
            Directory.CreateDirectory(Path.GetDirectoryName(driftPath)!);
            File.WriteAllText(driftPath, drift, Encoding.UTF8);
            Console.WriteLine($"Wrote drift report: {driftPath}");
        }

        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            v57-index — V57 Context Intelligence indexer (Roslyn)

            Usage:
              dotnet run --project V57/tools/roslyn-index -- --root <UnityProjectRoot> [options]

            Options:
              --root <path>           Unity project root (default: current directory)
              --output <path>         Output JSON (default: <root>/project-index.json)
              --propose-context       Write CONTEXT.md auto-block patch proposal
              --context-patch <path>  Patch output path
              --drift                 Write drift-report.md (spec touches vs disk vs index)
              --drift-output <path>   Drift report path
              --help                  Show this help

            See V57/docs/context/CONTEXT_INDEX.md
            """);
    }
}

internal sealed class CliOptions
{
    public string ProjectRoot { get; init; } = Directory.GetCurrentDirectory();
    public string? OutputPath { get; init; }
    public bool ProposeContext { get; init; }
    public string? ContextPatchPath { get; init; }
    public bool DriftReport { get; init; }
    public string? DriftReportPath { get; init; }
    public bool ShowHelp { get; init; }

    public static CliOptions Parse(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        string? output = null;
        string? contextPatch = null;
        string? driftOutput = null;
        var proposeContext = false;
        var drift = false;
        var help = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--root":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --root requires a path value.");
                        Environment.Exit(1);
                    }

                    root = Path.GetFullPath(args[++i]);
                    break;
                case "--output":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --output requires a path value.");
                        Environment.Exit(1);
                    }

                    output = Path.GetFullPath(args[++i]);
                    break;
                case "--propose-context":
                    proposeContext = true;
                    break;
                case "--context-patch":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --context-patch requires a path value.");
                        Environment.Exit(1);
                    }

                    contextPatch = Path.GetFullPath(args[++i]);
                    break;
                case "--drift":
                    drift = true;
                    break;
                case "--drift-output":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --drift-output requires a path value.");
                        Environment.Exit(1);
                    }

                    driftOutput = Path.GetFullPath(args[++i]);
                    break;
                case "--help":
                case "-h":
                    help = true;
                    break;
                default:
                    Console.Error.WriteLine($"Error: unknown argument '{arg}'. Use --help for usage.");
                    Environment.Exit(1);
                    break;
            }
        }

        return new CliOptions
        {
            ProjectRoot = root,
            OutputPath = output,
            ProposeContext = proposeContext,
            ContextPatchPath = contextPatch,
            DriftReport = drift,
            DriftReportPath = driftOutput,
            ShowHelp = help,
        };
    }
}

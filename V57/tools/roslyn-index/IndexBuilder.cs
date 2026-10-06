using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace V57.RoslynIndex;

internal sealed class IndexBuilder
{
    private readonly string _projectRoot;
    private readonly string _scriptsRoot;
    private readonly Dictionary<string, string> _classToFile = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _fileToClasses = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _classReferences = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _interfaceImplementors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _interfaceConsumers = new(StringComparer.Ordinal);
    private readonly List<SpecTouchInfo> _specs = [];

    public IReadOnlyList<SpecTouchInfo> Specs => _specs;

    public IndexBuilder(string projectRoot)
    {
        _projectRoot = projectRoot;
        _scriptsRoot = ResolveScriptsRoot(projectRoot);
    }

    /// <summary>V57 layout: Assets/_Game/Scripts (agents.yaml layout.v57_owned); legacy Assets/Scripts as fallback.</summary>
    public static string ResolveScriptsRoot(string projectRoot)
    {
        var game = Path.Combine(projectRoot, "Assets", "_Game", "Scripts");
        if (Directory.Exists(game))
        {
            return game;
        }

        return Path.Combine(projectRoot, "Assets", "Scripts");
    }

    public ProjectIndex Build()
    {
        LoadSpecs();
        IndexScripts();

        var index = new ProjectIndex
        {
            GeneratedAt = DateTime.UtcNow.ToString("o"),
        };

        BuildModules(index);
        BuildInterfaces(index);
        BuildEvents(index);

        return index;
    }

    public void MergeUnityAssets(ProjectIndex index, string unityAssetsPath)
    {
        try
        {
            var json = File.ReadAllText(unityAssetsPath);
            var export = JsonSerializer.Deserialize<UnityAssetsExport>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (export == null)
            {
                Console.Error.WriteLine($"Warning: could not deserialize Unity assets export: {unityAssetsPath}");
                return;
            }

            index.Assets.Prefabs = export.Prefabs;
            index.Assets.ScriptableObjects = export.ScriptableObjects;
            index.Assets.Scenes = export.Scenes;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: failed to merge Unity assets from '{unityAssetsPath}': {ex.Message}");
        }
    }

    private void LoadSpecs()
    {
        var specsRoot = Path.Combine(_projectRoot, "V57", "specs");
        if (!Directory.Exists(specsRoot))
        {
            return;
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        foreach (var path in Directory.EnumerateFiles(specsRoot, "*.yaml", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}template{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Path.GetFileName(path).StartsWith("_tdd_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var yaml = File.ReadAllText(path);
                var dict = deserializer.Deserialize<Dictionary<object, object>>(yaml);
                if (dict == null)
                {
                    continue;
                }

                var info = new SpecTouchInfo
                {
                    SpecPath = ToProjectRelative(path),
                    ModuleName = GetString(dict, "name"),
                    SpecId = GetString(dict, "specId"),
                };

                if (TryGetNestedDict(dict, "touches", out var touches))
                {
                    info.TouchScripts = GetStringList(touches, "scripts");
                    info.TouchPrefabs = GetStringList(touches, "prefabs");
                    info.TouchScriptableObjects = GetStringList(touches, "scriptable_objects");
                    info.TouchTests = GetStringList(touches, "tests");
                }

                if (string.IsNullOrWhiteSpace(info.SpecId) && !string.IsNullOrWhiteSpace(info.ModuleName))
                {
                    info.SpecId = ToSnakeCase(info.ModuleName);
                }

                _specs.Add(info);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: skipping malformed spec '{path}': {ex.Message}");
            }
        }
    }

    private void IndexScripts()
    {
        if (!Directory.Exists(_scriptsRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_scriptsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".Tests.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relative = ToProjectRelative(file);
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text, path: file);
            var root = tree.GetRoot();

            var classes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text is not ("Tests" or "Test"))
                .ToList();

            var localTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cls in classes)
            {
                var name = cls.Identifier.Text;
                localTypes.Add(name);
                if (_classToFile.TryGetValue(name, out var existingFile) &&
                    !string.Equals(existingFile, relative, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine(
                        $"Warning: duplicate class '{name}' in '{relative}' (already indexed from '{existingFile}')");
                }

                _classToFile[name] = relative;
                if (!_fileToClasses.ContainsKey(relative))
                {
                    _fileToClasses[relative] = new HashSet<string>(StringComparer.Ordinal);
                }

                _fileToClasses[relative].Add(name);

                foreach (var baseType in cls.BaseList?.Types ?? Enumerable.Empty<BaseTypeSyntax>())
                {
                    var typeName = baseType.Type.ToString();
                    if (typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]) &&
                        typeName is not ("IEnumerator" or "IDisposable" or "IEnumerable"))
                    {
                        if (!_interfaceImplementors.ContainsKey(typeName))
                        {
                            _interfaceImplementors[typeName] = new HashSet<string>(StringComparer.Ordinal);
                        }

                        _interfaceImplementors[typeName].Add(name);
                    }
                }
            }

            var refs = ExtractTypeReferences(root, localTypes);
            foreach (var cls in classes)
            {
                var name = cls.Identifier.Text;
                if (!_classReferences.ContainsKey(name))
                {
                    _classReferences[name] = new HashSet<string>(StringComparer.Ordinal);
                }

                foreach (var r in refs)
                {
                    if (_classToFile.ContainsKey(r) && r != name)
                    {
                        _classReferences[name].Add(r);
                    }

                    if (r.StartsWith('I') && r.Length > 1 && char.IsUpper(r[1]) &&
                        r is not ("IEnumerator" or "IDisposable" or "IEnumerable"))
                    {
                        if (!_interfaceConsumers.ContainsKey(r))
                        {
                            _interfaceConsumers[r] = new HashSet<string>(StringComparer.Ordinal);
                        }

                        _interfaceConsumers[r].Add(name);
                    }
                }
            }
        }
    }

    private static HashSet<string> ExtractTypeReferences(SyntaxNode root, HashSet<string> localTypes)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                case IdentifierNameSyntax id when localTypes.Contains(id.Identifier.Text) == false:
                    if (char.IsUpper(id.Identifier.Text[0]))
                    {
                        result.Add(id.Identifier.Text);
                    }

                    break;
                case GenericNameSyntax generic:
                    result.Add(generic.Identifier.Text);
                    break;
            }
        }

        return result;
    }

    private void BuildModules(ProjectIndex index)
    {
        var assignedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in _specs)
        {
            if (string.IsNullOrWhiteSpace(spec.ModuleName))
            {
                continue;
            }

            var scripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var touch in spec.TouchScripts)
            {
                scripts.Add(NormalizeAssetPath(touch));
            }

            foreach (var file in spec.TouchScripts)
            {
                assignedFiles.Add(NormalizeAssetPath(file));
            }

            foreach (var kvp in _fileToClasses)
            {
                if (spec.TouchScripts.Count == 0 &&
                    kvp.Value.Any(c => c.Contains(spec.ModuleName, StringComparison.OrdinalIgnoreCase) ||
                                       spec.ModuleName.Contains(c, StringComparison.OrdinalIgnoreCase)))
                {
                    scripts.Add(kvp.Key);
                    assignedFiles.Add(kvp.Key);
                }
            }

            var classes = scripts
                .SelectMany(s => _fileToClasses.GetValueOrDefault(s) ?? [])
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var deps = classes
                .SelectMany(c => _classReferences.GetValueOrDefault(c) ?? [])
                .Where(d => !classes.Contains(d, StringComparer.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            index.Modules.Add(new ModuleEntry
            {
                Id = spec.ModuleName,
                SpecId = spec.SpecId,
                Scripts = scripts.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                DependsOn = deps,
                Classes = classes,
            });
        }

        var orphanFiles = _fileToClasses.Keys
            .Where(f => !assignedFiles.Contains(f))
            .Where(f => !f.Contains("/Tests/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (orphanFiles.Count > 0)
        {
            index.Modules.Add(new ModuleEntry
            {
                Id = "_Unassigned",
                SpecId = "_unassigned",
                Scripts = orphanFiles.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                Classes = orphanFiles
                    .SelectMany(f => _fileToClasses[f])
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList(),
            });
        }
    }

    private void BuildInterfaces(ProjectIndex index)
    {
        var allInterfaces = _interfaceImplementors.Keys
            .Union(_interfaceConsumers.Keys, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal);

        foreach (var iface in allInterfaces)
        {
            index.Interfaces.Add(new InterfaceEntry
            {
                Name = iface,
                Implementors = (_interfaceImplementors.GetValueOrDefault(iface) ?? [])
                    .OrderBy(x => x, StringComparer.Ordinal).ToList(),
                Consumers = (_interfaceConsumers.GetValueOrDefault(iface) ?? [])
                    .OrderBy(x => x, StringComparer.Ordinal).ToList(),
            });
        }
    }

    private void BuildEvents(ProjectIndex index)
    {
        var eventClasses = _classToFile.Keys
            .Where(n => n.EndsWith("Event", StringComparison.Ordinal) ||
                        n.EndsWith("EventChannel", StringComparison.Ordinal))
            .ToList();

        foreach (var eventName in eventClasses.OrderBy(x => x, StringComparer.Ordinal))
        {
            var publishers = new HashSet<string>(StringComparer.Ordinal);
            var subscribers = new HashSet<string>(StringComparer.Ordinal);

            foreach (var kvp in _classReferences)
            {
                if (kvp.Value.Contains(eventName, StringComparer.Ordinal))
                {
                    subscribers.Add(kvp.Key);
                }
            }

            if (_classToFile.TryGetValue(eventName, out _))
            {
                publishers.Add(eventName);
            }

            index.Events.Add(new EventEntry
            {
                Name = eventName,
                Publishers = publishers.OrderBy(x => x, StringComparer.Ordinal).ToList(),
                Subscribers = subscribers.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            });
        }

        if (_classToFile.ContainsKey("EventBus"))
        {
            var busUsers = _classReferences
                .Where(kvp => kvp.Value.Contains("EventBus", StringComparer.Ordinal))
                .Select(kvp => kvp.Key)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            index.Events.Add(new EventEntry
            {
                Name = "EventBus",
                Publishers = busUsers,
                Subscribers = busUsers,
            });
        }
    }

    private string ToProjectRelative(string absolutePath)
    {
        var rel = Path.GetRelativePath(_projectRoot, absolutePath);
        return rel.Replace('\\', '/');
    }

    private static string NormalizeAssetPath(string path) =>
        path.Replace('\\', '/').Trim();

    private static string? GetString(Dictionary<object, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }

        return null;
    }

    private static bool TryGetNestedDict(Dictionary<object, object> dict, string key, out Dictionary<object, object> nested)
    {
        nested = new Dictionary<object, object>();
        if (!dict.TryGetValue(key, out var value) || value is not Dictionary<object, object> dictValue)
        {
            return false;
        }

        nested = dictValue;
        return true;
    }

    private static List<string> GetStringList(Dictionary<object, object> dict, string key)
    {
        var snake = key;
        var camel = key switch
        {
            "scriptable_objects" => "scriptableObjects",
            _ => key,
        };

        if (dict.TryGetValue(snake, out var value) || dict.TryGetValue(camel, out value))
        {
            if (value is IEnumerable<object> list)
            {
                return list.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
        }

        return [];
    }

    private static string ToSnakeCase(string name) =>
        Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}

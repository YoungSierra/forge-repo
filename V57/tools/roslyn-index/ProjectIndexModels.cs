namespace V57.RoslynIndex;

internal sealed class ProjectIndex
{
    public string IndexVersion { get; set; } = "1.0";
    public string GeneratedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public string Generator { get; set; } = "v57-index/1.0.0";
    public List<ModuleEntry> Modules { get; set; } = [];
    public List<InterfaceEntry> Interfaces { get; set; } = [];
    public List<EventEntry> Events { get; set; } = [];
    public AssetIndex Assets { get; set; } = new();
}

internal sealed class ModuleEntry
{
    public string Id { get; set; } = "";
    public string? SpecId { get; set; }
    public List<string> Scripts { get; set; } = [];
    public List<string> DependsOn { get; set; } = [];
    public List<string> Classes { get; set; } = [];
}

internal sealed class InterfaceEntry
{
    public string Name { get; set; } = "";
    public List<string> Implementors { get; set; } = [];
    public List<string> Consumers { get; set; } = [];
}

internal sealed class EventEntry
{
    public string Name { get; set; } = "";
    public List<string> Publishers { get; set; } = [];
    public List<string> Subscribers { get; set; } = [];
}

internal sealed class AssetIndex
{
    public List<PrefabEntry> Prefabs { get; set; } = [];
    public List<ScriptableObjectEntry> ScriptableObjects { get; set; } = [];
    public List<SceneEntry> Scenes { get; set; } = [];
}

internal sealed class PrefabEntry
{
    public string Path { get; set; } = "";
    public List<string> Components { get; set; } = [];
}

internal sealed class ScriptableObjectEntry
{
    public string Path { get; set; } = "";
    public string? ScriptType { get; set; }
}

internal sealed class SceneEntry
{
    public string Path { get; set; } = "";
    public List<string> RootObjects { get; set; } = [];
}

internal sealed class SpecTouchInfo
{
    public string SpecPath { get; set; } = "";
    public string? SpecId { get; set; }
    public string? ModuleName { get; set; }
    public List<string> TouchScripts { get; set; } = [];
    public List<string> TouchPrefabs { get; set; } = [];
    public List<string> TouchScriptableObjects { get; set; } = [];
    public List<string> TouchTests { get; set; } = [];
}

internal sealed class UnityAssetsExport
{
    public string ExportVersion { get; set; } = "1.0";
    public string GeneratedAt { get; set; } = "";
    public List<PrefabEntry> Prefabs { get; set; } = [];
    public List<ScriptableObjectEntry> ScriptableObjects { get; set; } = [];
    public List<SceneEntry> Scenes { get; set; } = [];
}

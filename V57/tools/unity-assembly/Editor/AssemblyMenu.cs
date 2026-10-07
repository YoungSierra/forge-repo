using UnityEditor;

namespace V57.Assembly
{
    /// <summary>Editor menu mirrors of the <see cref="AssemblyRunner"/> entry points (humans; agents use the CLI).</summary>
    public static class AssemblyMenu
    {
        private const string Root = "Tools/V57/Assembly/";

        [MenuItem(Root + "Run All", priority = 0)]
        private static void RunAll() => AssemblyRunner.RunAll();

        [MenuItem(Root + "Run All (Force Rebuild)", priority = 1)]
        private static void RunAllForce() => AssemblyRunner.RunAllForce();

        [MenuItem(Root + "Steps/Apply Import Rules", priority = 20)]
        private static void ApplyImportRules() => AssemblyRunner.ApplyImportRules();

        [MenuItem(Root + "Steps/Build Materials", priority = 21)]
        private static void BuildMaterials() => AssemblyRunner.BuildMaterials();

        [MenuItem(Root + "Steps/Build Skybox", priority = 22)]
        private static void BuildSkybox() => AssemblyRunner.BuildSkybox();

        [MenuItem(Root + "Steps/Build UI Atlases", priority = 23)]
        private static void BuildUiAtlases() => AssemblyRunner.BuildUiAtlases();

        [MenuItem(Root + "Steps/Build Animators", priority = 24)]
        private static void BuildAnimators() => AssemblyRunner.BuildAnimators();

        [MenuItem(Root + "Steps/Build Visual Prefabs", priority = 25)]
        private static void BuildVisualPrefabs() => AssemblyRunner.BuildVisualPrefabs();

        [MenuItem(Root + "Steps/Build Level Data", priority = 26)]
        private static void BuildLevelData() => AssemblyRunner.BuildLevelData();

        [MenuItem(Root + "Steps/Build Level Scenes", priority = 27)]
        private static void BuildLevelScenes() => AssemblyRunner.BuildLevelScenes();

        [MenuItem(Root + "Capture Hierarchy Diff", priority = 40)]
        private static void CaptureHierarchyDiff() => AssemblyRunner.CaptureHierarchyDiff();

        [MenuItem(Root + "Write Report", priority = 41)]
        private static void WriteReport() => AssemblyRunner.WriteReport();
    }
}

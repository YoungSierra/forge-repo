#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace V57.EditorTools
{
    /// <summary>
    /// Exports prefab, ScriptableObject and scene wiring for V57 Context Intelligence.
    /// Install: copy this file to Assets/Editor/V57/V57IndexExporter.cs
    /// Menu: V57 / Export Unity Assets Index
    /// </summary>
    public static class V57IndexExporter
    {
        private const string OutputFileName = "v57-unity-assets.json";

        [MenuItem("V57/Export Unity Assets Index")]
        public static void Export()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("V57IndexExporter: could not resolve project root.");
                return;
            }

            var export = new ExportRoot
            {
                exportVersion = "1.0",
                generatedAt = System.DateTime.UtcNow.ToString("o"),
                prefabs = CollectPrefabs(),
                scriptableObjects = CollectScriptableObjects(),
                scenes = CollectScenes(),
            };

            var json = JsonUtility.ToJson(export, true);
            var outputPath = Path.Combine(projectRoot, OutputFileName);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
            Debug.Log($"V57IndexExporter: wrote {outputPath} ({export.prefabs.Count} prefabs, {export.scriptableObjects.Count} SO, {export.scenes.Count} scenes)");
        }

        private static List<PrefabEntry> CollectPrefabs()
        {
            var result = new List<PrefabEntry>();
            var searchFolders = ResolveSearchFolders("Assets/Prefabs");
            var guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                var components = prefab.GetComponentsInChildren<Component>(true)
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                result.Add(new PrefabEntry { path = path, components = components });
            }

            return result.OrderBy(p => p.path).ToList();
        }

        private static List<ScriptableObjectEntry> CollectScriptableObjects()
        {
            var result = new List<ScriptableObjectEntry>();
            var searchFolders = ResolveSearchFolders("Assets/ScriptableObjects");
            var guids = AssetDatabase.FindAssets("t:ScriptableObject", searchFolders);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue;
                }

                result.Add(new ScriptableObjectEntry
                {
                    path = path,
                    scriptType = asset.GetType().Name,
                });
            }

            return result.OrderBy(s => s.path).ToList();
        }

        private static List<SceneEntry> CollectScenes()
        {
            var result = new List<SceneEntry>();
            var searchFolders = ResolveSearchFolders("Assets/Scenes");
            var guids = AssetDatabase.FindAssets("t:Scene", searchFolders);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var rootObjects = new List<string>();

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root != null)
                    {
                        rootObjects.Add(root.name);
                    }
                }

                EditorSceneManager.CloseScene(scene, true);
                rootObjects.Sort();

                result.Add(new SceneEntry { path = path, rootObjects = rootObjects });
            }

            return result.OrderBy(s => s.path).ToList();
        }

        private static string[] ResolveSearchFolders(string preferredFolder)
        {
            if (AssetDatabase.IsValidFolder(preferredFolder))
            {
                return new[] { preferredFolder };
            }

            Debug.LogWarning(
                $"V57IndexExporter: folder '{preferredFolder}' not found; falling back to full Assets/ scan.");
            return new[] { "Assets" };
        }

        [System.Serializable]
        private sealed class ExportRoot
        {
            public string exportVersion;
            public string generatedAt;
            public List<PrefabEntry> prefabs = new();
            public List<ScriptableObjectEntry> scriptableObjects = new();
            public List<SceneEntry> scenes = new();
        }

        [System.Serializable]
        private sealed class PrefabEntry
        {
            public string path;
            public List<string> components = new();
        }

        [System.Serializable]
        private sealed class ScriptableObjectEntry
        {
            public string path;
            public string scriptType;
        }

        [System.Serializable]
        private sealed class SceneEntry
        {
            public string path;
            public List<string> rootObjects;
        }
    }
}
#endif

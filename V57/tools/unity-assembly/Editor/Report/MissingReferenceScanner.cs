using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace V57.Assembly.Report
{
    /// <summary>
    /// Finds missing scripts and broken object references (reference with a non-zero instance id that no longer
    /// resolves) in prefabs and scenes created by the assembly steps.
    /// </summary>
    public static class MissingReferenceScanner
    {
        #region Public Methods

        public static List<MissingRefEntry> Scan(IReadOnlyList<string> assetPaths)
        {
            List<MissingRefEntry> found = new List<MissingRefEntry>();
            foreach (string path in assetPaths)
            {
                if (path.EndsWith(".prefab"))
                {
                    ScanPrefab(path, found);
                }
                else if (path.EndsWith(".unity"))
                {
                    ScanScene(path, found);
                }
            }

            return found;
        }

        #endregion

        #region Private Methods

        private static void ScanPrefab(string path, List<MissingRefEntry> found)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ScanHierarchy(path, root.transform, root.name, found);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ScanScene(string path, List<MissingRefEntry> found)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedHere = !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    ScanHierarchy(path, root.transform, root.name, found);
                }
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ScanHierarchy(string asset, Transform node, string nodePath, List<MissingRefEntry> found)
        {
            foreach (Component component in node.GetComponents<Component>())
            {
                if (component == null)
                {
                    found.Add(new MissingRefEntry { asset = asset, object_path = nodePath, component = "<missing script>", property = string.Empty });
                    continue;
                }

                SerializedObject serialized = new SerializedObject(component);
                SerializedProperty property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue == null
                        && HasDanglingReference(property))
                    {
                        found.Add(new MissingRefEntry { asset = asset, object_path = nodePath, component = component.GetType().Name, property = property.propertyPath });
                    }
                }
            }

            foreach (Transform child in node)
            {
                ScanHierarchy(asset, child, nodePath + "/" + child.name, found);
            }
        }

        /// <summary>True when the property stores a reference id but the object no longer resolves (missing asset).</summary>
        private static bool HasDanglingReference(SerializedProperty property)
        {
#if UNITY_6000_3_OR_NEWER
            return !property.objectReferenceEntityIdValue.Equals(default(EntityId));
#else
            return property.objectReferenceInstanceIDValue != 0;
#endif
        }

        #endregion
    }
}

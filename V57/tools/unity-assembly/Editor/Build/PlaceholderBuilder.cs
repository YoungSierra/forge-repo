using System;
using UnityEditor;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;
using V57.GoldPath;

namespace V57.Assembly.Build
{
    /// <summary>
    /// For visual assets without a model on disk, writes the Visual prefab path with a primitive sized to size_m,
    /// offset for the manifest pivot, MAT_V57_Placeholder, a <c>MissingAsset: &lt;asset_name&gt;</c> TextMesh label and a
    /// <see cref="V57Placeholder"/> tag — so gameplay variants can be built now and real art swaps in later.
    /// </summary>
    public static class PlaceholderBuilder
    {
        #region Fields

        private const string Step = "BuildPlaceholders";

        #endregion

        #region Public Methods

        public static void BuildAll()
        {
            AssetManifestDto manifest = GeneratedData.AssetManifest;
            if (manifest?.assets == null)
            {
                AssemblyContext.Error(Step, "asset_manifest.json missing or empty");
                return;
            }

            Material material = PlaceholderMaterial.GetOrCreate();
            foreach (AssetEntryDto entry in manifest.assets)
            {
                if (entry == null || !VisualPrefabPaths.IsVisualType(entry) || VisualPrefabPaths.ResolveModelPath(entry) != null)
                {
                    continue;
                }

                string prefabPath = VisualPrefabPaths.PrefabPath(entry, null);
                if (VisualPrefabPaths.Exists(prefabPath) && !VisualPrefabPaths.IsPlaceholder(prefabPath))
                {
                    AssemblyContext.Warn(Step, $"{prefabPath} exists and is not a placeholder; left untouched");
                    continue;
                }

                Build(entry, prefabPath, material);
            }
        }

        #endregion

        #region Private Methods

        private static void Build(AssetEntryDto entry, string prefabPath, Material material)
        {
            string name = VisualPrefabPaths.SafeName(entry);
            Vector3 size = SizeOf(entry);
            bool character = string.Equals(entry.type, "character", StringComparison.OrdinalIgnoreCase);
            GameObject root = new GameObject($"PRF_{name}_Visual");
            try
            {
                GameObject body = GameObject.CreatePrimitive(character ? PrimitiveType.Capsule : PrimitiveType.Cube);
                body.name = "Placeholder_Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localScale = character ? new Vector3(size.x, size.y * 0.5f, size.z) : size;
                body.transform.localPosition = PivotOffset(entry, size);
                body.GetComponent<Renderer>().sharedMaterial = material;
                string collision = (entry.collision ?? string.Empty).ToLowerInvariant();
                bool wantsCollider = collision == "simple" || collision == "exact";
                if (!wantsCollider || AssemblyPhysics.Is2D)
                {
                    UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
                }

                if (wantsCollider && AssemblyPhysics.Is2D)
                {
                    BoxCollider2D box = root.AddComponent<BoxCollider2D>();
                    box.offset = new Vector2(body.transform.localPosition.x, body.transform.localPosition.y);
                    box.size = new Vector2(size.x, size.y);
                }

                float top = body.transform.localPosition.y + size.y * 0.5f;
                CreateLabel(root.transform, $"MissingAsset: {name}\n{entry.asset_id}", top, size.y);
                root.AddComponent<V57Placeholder>().Configure(entry.asset_id, entry.asset_name, "model file not found");
                AssemblyPaths.EnsureFolder(AssemblyPaths.ParentFolder(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success)
                {
                    AssemblyContext.Error(Step, $"{prefabPath}: SaveAsPrefabAsset failed");
                    return;
                }

                AssemblyContext.Counts.placeholders++;
                AssemblyContext.TrackAsset(prefabPath);
                AssemblyContext.Warn(Step, $"placeholder for {entry.asset_id} ({name}) → {prefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Vector3 SizeOf(AssetEntryDto entry)
        {
            if (entry.size_m == null || entry.size_m.Length < 3)
            {
                return Vector3.one;
            }

            return new Vector3(Mathf.Max(0.05f, entry.size_m[0]), Mathf.Max(0.05f, entry.size_m[1]), Mathf.Max(0.05f, entry.size_m[2]));
        }

        private static Vector3 PivotOffset(AssetEntryDto entry, Vector3 size)
        {
            string pivot = (entry.pivot ?? string.Empty).ToLowerInvariant();
            string side = (entry.side ?? string.Empty).ToLowerInvariant();
            switch (pivot)
            {
                case "center":
                case "axle":
                    return Vector3.zero;
                case "hinge":
                    return new Vector3(side == "right" ? -size.x * 0.5f : size.x * 0.5f, 0f, 0f);
                default:
                    return new Vector3(0f, size.y * 0.5f, 0f); // feet/base (default)
            }
        }

        private static void CreateLabel(Transform parent, string text, float top, float height)
        {
            GameObject label = new GameObject("Placeholder_Label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(0f, top + 0.1f, 0f);
            MeshRenderer renderer = label.AddComponent<MeshRenderer>();
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.LowerCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 48;
            mesh.characterSize = Mathf.Clamp(height * 0.05f, 0.02f, 0.5f);
            mesh.color = Color.white;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                mesh.font = font;
                renderer.sharedMaterial = font.material;
            }
        }

        #endregion
    }
}

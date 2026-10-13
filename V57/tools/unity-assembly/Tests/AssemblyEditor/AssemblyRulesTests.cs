using NUnit.Framework;
using UnityEngine;
using V57.Assembly.Build;
using V57.Assembly.Data;

namespace V57.Assembly.Tests
{
    public sealed class AssemblyRulesTests
    {
        [TestCase("Professor_Wort_albedo", "BC")]
        [TestCase("AST-ENV-PIPE-001_AST-ENV-PIPE-001_Albedo", "BC")]
        [TestCase("Sprat_normal", "N")]
        [TestCase("Fly_MetallicSmoothness", "MS")]
        [TestCase("Y_Prop_metallic", "M")]
        [TestCase("Y_Prop_roughness", "R")]
        [TestCase("Crate_BaseColor", "BC")]
        [TestCase("Crate_AO", "AO")]
        public void ReadsDccTextureSuffixes(string stem, string expected)
        {
            Assert.IsTrue(AssetNaming.TrySplitDccTexture(stem, out string suffix));
            Assert.AreEqual(expected, suffix);
        }

        [TestCase("T_Koala_BC")]
        [TestCase("SkyBox_1")]
        [TestCase("albedo")]
        [TestCase("AST-ENV-SEABED-001_Detail_Albedo")]
        [TestCase("Rock_detail_normal")]
        public void IgnoresContractAndUnknownNames(string stem)
        {
            Assert.IsFalse(AssetNaming.TrySplitDccTexture(stem, out _));
        }

        [Test]
        public void DefaultAnimatorStateIsLoopingIdle()
        {
            Assert.AreEqual(2, AnimatorControllerBuilder.PickDefaultIndex(new[] { "Attack", "Death", "Idle", "Run" }, new[] { false, false, true, true }));
            Assert.AreEqual(1, AnimatorControllerBuilder.PickDefaultIndex(new[] { "EyeRotation", "PassiveTransit" }, new[] { false, true }));
            Assert.AreEqual(0, AnimatorControllerBuilder.PickDefaultIndex(new[] { "PopOut", "Spin" }, new[] { false, false }));
            Assert.AreEqual(-1, AnimatorControllerBuilder.PickDefaultIndex(new string[0], new bool[0]));
        }

        [Test]
        public void ParsesNormalizedLayouts()
        {
            const string json = "{\"layouts\":[{\"level_id\":\"Montage\",\"source\":\"Docs/Design/LevelMaps/Montage/unity_scene.json\"," +
                "\"objects\":[{\"name\":\"Jellyfish_01\",\"asset_id\":\"Jellyfish\",\"model\":\"Assets/_Game/Art/Characters/Jellyfish/Meshes/SK_Jellyfish.fbx\"," +
                "\"layer\":\"Ocean\",\"position\":[1,2,3],\"rotation\":[0,0.7071,0,0.7071],\"scale\":[1,1,1]}]," +
                "\"materials\":[{\"asset_id\":\"Jellyfish\",\"name\":\"Jellyfish\",\"base_color\":[1,0.5,0,1],\"metallic\":0,\"smoothness\":0.5,\"double_sided\":true}]}]}";
            LayoutsDto data = JsonUtility.FromJson<LayoutsDto>(json);
            Assert.AreEqual("Montage", data.layouts[0].level_id);
            Assert.AreEqual("Ocean", data.layouts[0].objects[0].layer);
            Assert.AreEqual(3f, data.layouts[0].objects[0].position[2]);
            Assert.IsTrue(data.layouts[0].materials[0].double_sided);
        }

        [Test]
        public void ParsesAssetRuleFields()
        {
            const string json = "{\"assets\":[{\"asset_id\":\"INV-Crate\",\"asset_name\":\"Crate\",\"type\":\"model\",\"collision\":\"convex\",\"ruled\":true," +
                "\"shadows\":\"two_sided\",\"shader\":\"Game/Wobble\",\"attach\":[{\"prefab\":\"Assets/_Game/Prefabs/VFX/VFX_Spark.prefab\",\"offset\":[0,0.5,0],\"align\":\"instance\"}]}],\"orphans\":[]}";
            AssetEntryDto entry = JsonUtility.FromJson<AssetManifestDto>(json).assets[0];
            Assert.IsTrue(entry.ruled);
            Assert.AreEqual("two_sided", entry.shadows);
            Assert.AreEqual("Game/Wobble", entry.shader);
            Assert.AreEqual("instance", entry.attach[0].align);
            Assert.AreEqual(0.5f, entry.attach[0].offset[1]);
        }

        [Test]
        public void RuleCollisionAndShadowsOnLod0Meshes()
        {
            GameObject root = new GameObject("PRF_Crate_Visual");
            Mesh mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            foreach (string lod in new[] { "Crate_LOD0", "Crate_LOD1" })
            {
                GameObject node = new GameObject(lod, typeof(MeshFilter), typeof(MeshRenderer));
                node.transform.SetParent(root.transform, false);
                node.GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            try
            {
                AssetEntryDto entry = new AssetEntryDto { asset_name = "Crate", type = "model", collision = "convex", shadows = "two_sided" };
                Assert.AreEqual("convex(1)", PrefabColliderBuilder.Apply(entry, root, new Bounds(Vector3.zero, Vector3.one)));
                MeshCollider collider = root.GetComponentInChildren<MeshCollider>();
                Assert.AreEqual("Crate_LOD0", collider.name);
                Assert.IsTrue(collider.convex);

                Assert.IsTrue(AssetRulesApplier.ApplyShadows(entry, root));
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                {
                    Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.TwoSided, renderer.shadowCastingMode);
                }

                Assert.IsFalse(AssetRulesApplier.ApplyShadows(entry, root), "second pass changes nothing");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}

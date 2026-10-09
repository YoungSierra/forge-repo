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
    }
}

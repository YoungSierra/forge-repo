using NUnit.Framework;

namespace V57.GoldPath.Tests
{
    public sealed class NamingTests
    {
        [TestCase("Marker_Spawn_Player", V57MarkerKind.Spawn, "Player")]
        [TestCase("Marker_Patrol_Guard_02", V57MarkerKind.Patrol, "Guard_02")]
        [TestCase("Marker_Bounds", V57MarkerKind.Bounds, "Bounds")]
        [TestCase("Marker_Camera_Top", V57MarkerKind.Camera, "Top")]
        [TestCase("Marker_Foo_Bar", V57MarkerKind.Other, "Foo_Bar")]
        public void ParsesMarkerNames(string name, V57MarkerKind kind, string id)
        {
            Assert.IsTrue(V57MarkerNaming.TryParse(name, out V57MarkerKind parsedKind, out string parsedId));
            Assert.AreEqual(kind, parsedKind);
            Assert.AreEqual(id, parsedId);
        }

        [Test]
        public void RejectsNonMarkers()
        {
            Assert.IsFalse(V57MarkerNaming.TryParse("UCX_Box_01", out V57MarkerKind kind, out string id));
        }

        [TestCase("Level01", "Assets/_Game/Scenes/SCN_Level01.unity")]
        [TestCase("SCN_Level01", "Assets/_Game/Scenes/SCN_Level01.unity")]
        [TestCase("Woodland Pond", "Assets/_Game/Scenes/SCN_Woodland_Pond.unity")]
        [TestCase("Assets/_Game/Scenes/SCN_X.unity", "Assets/_Game/Scenes/SCN_X.unity")]
        public void MapsSceneIds(string id, string path)
        {
            Assert.AreEqual(path, V57SceneNaming.ToScenePath(id));
        }

        [TestCase("space", "<Keyboard>/space")]
        [TestCase("Gamepad/buttonSouth", "<Gamepad>/buttonSouth")]
        [TestCase("<Mouse>/leftButton", "<Mouse>/leftButton")]
        [TestCase("Player/Jump", "Player/Jump")]
        public void NormalizesControlPaths(string input, string expected)
        {
            Assert.AreEqual(expected, GoldPathControlPath.Normalize(input));
        }
    }
}

using System;
using System.IO;
using UnityEngine;

namespace V57.GoldPath.Tests
{
    /// <summary>Resolves the first slice scene from <c>package.json → slice.scenes</c>, else <c>scenes.json</c> (slice: true).</summary>
    public static class GoldPathTestScenes
    {
        #region Nested Types

        [Serializable]
        public sealed class PackageSlice
        {
            public SliceBlock slice;
        }

        [Serializable]
        public sealed class SliceBlock
        {
            public string[] scenes;
        }

        [Serializable]
        public sealed class SceneList
        {
            public SceneItem[] scenes;
        }

        [Serializable]
        public sealed class SceneItem
        {
            public string id;
            public bool slice;
        }

        private static T Read<T>(string path) where T : class
        {
            return File.Exists(path) ? JsonUtility.FromJson<T>(JsonPreprocessor.Process(File.ReadAllText(path))) : null;
        }

        #endregion

        #region Public Methods

        public static string FirstSliceScenePath()
        {
            string directory = Path.Combine(GoldPathPaths.ProjectRoot, "Docs/Generated/json");
            PackageSlice package = Read<PackageSlice>(Path.Combine(directory, "package.json"));
            if (package?.slice?.scenes != null && package.slice.scenes.Length > 0 && !string.IsNullOrEmpty(package.slice.scenes[0]))
            {
                return V57SceneNaming.ToScenePath(package.slice.scenes[0]);
            }

            SceneList list = Read<SceneList>(Path.Combine(directory, "scenes.json"));
            foreach (SceneItem item in list?.scenes ?? new SceneItem[0])
            {
                if (item != null && item.slice && !string.IsNullOrEmpty(item.id))
                {
                    return V57SceneNaming.ToScenePath(item.id);
                }
            }

            return null;
        }

        #endregion
    }
}

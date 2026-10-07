using System;
using System.IO;
using UnityEngine;
using V57.GoldPath;

namespace V57.Assembly.Data
{
    /// <summary>
    /// Loads <c>Docs/Generated/json/*.json</c> (written by V57 intake) into JsonUtility DTOs.
    /// Null members are stripped first (<see cref="JsonPreprocessor"/>). Missing or invalid files yield null
    /// plus an entry in <see cref="LastLoadProblems"/>; callers must handle null.
    /// </summary>
    public static class GeneratedData
    {
        #region Fields

        private static readonly string[] QuotedScalarKeys = { "follow" };
        private static bool _loaded;
        private static PackageDto _package;
        private static AssetManifestDto _assetManifest;
        private static ScenesDto _scenes;
        private static UiDto _ui;
        private static CameraDto _camera;
        private static LayoutsDto _layouts;
        private static LevelDataIndexDto _levelData;
        private static string _lastLoadProblems = string.Empty;

        #endregion

        #region Public Methods

        public static PackageDto Package => EnsureLoaded(ref _package);

        public static AssetManifestDto AssetManifest => EnsureLoaded(ref _assetManifest);

        public static ScenesDto Scenes => EnsureLoaded(ref _scenes);

        public static UiDto Ui => EnsureLoaded(ref _ui);

        public static CameraDto Camera => EnsureLoaded(ref _camera);

        /// <summary>Optional: only present when the delivery has <c>Docs/Design/LevelMaps/&lt;LevelId&gt;/unity_scene.json</c>.</summary>
        public static LayoutsDto Layouts => EnsureLoaded(ref _layouts);

        /// <summary>Optional: only present when the delivery has <c>Docs/Design/LevelData/&lt;LevelId&gt;.json</c> files.</summary>
        public static LevelDataIndexDto LevelData => EnsureLoaded(ref _levelData);

        /// <summary>Layout for a LevelMaps level id, or null.</summary>
        public static LayoutDto FindLayout(string levelId)
        {
            foreach (LayoutDto layout in Layouts?.layouts ?? new LayoutDto[0])
            {
                if (layout != null && string.Equals(layout.level_id, levelId, StringComparison.OrdinalIgnoreCase))
                {
                    return layout;
                }
            }

            return null;
        }

        /// <summary>Semicolon-separated problems from the last <see cref="Reload"/>.</summary>
        public static string LastLoadProblems => _lastLoadProblems;

        public static void Reload()
        {
            _lastLoadProblems = string.Empty;
            _package = Load<PackageDto>("package");
            _assetManifest = Load<AssetManifestDto>("asset_manifest");
            _scenes = Load<ScenesDto>("scenes");
            _ui = Load<UiDto>("ui");
            _camera = Load<CameraDto>("camera");
            _layouts = File.Exists(PathOf("layouts")) ? Load<LayoutsDto>("layouts") : new LayoutsDto { layouts = new LayoutDto[0] };
            _levelData = File.Exists(PathOf("level_data")) ? Load<LevelDataIndexDto>("level_data") : new LevelDataIndexDto { levels = new LevelDataEntryDto[0] };
            _loaded = true;
        }

        public static string PathOf(string name)
        {
            return Path.Combine(AssemblyPaths.GeneratedJsonDirectory, name + ".json");
        }

        /// <summary>Loads one generated file; returns null when absent or invalid.</summary>
        public static T Load<T>(string name) where T : class
        {
            string path = PathOf(name);
            if (!File.Exists(path))
            {
                AddProblem($"{name}.json not found");
                return null;
            }

            try
            {
                string json = JsonPreprocessor.Process(File.ReadAllText(path), QuotedScalarKeys);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException)
            {
                AddProblem($"{name}.json unreadable: {exception.Message}");
                return null;
            }
        }

        #endregion

        #region Private Methods

        private static T EnsureLoaded<T>(ref T field) where T : class
        {
            if (!_loaded)
            {
                Reload();
            }

            return field;
        }

        private static void AddProblem(string problem)
        {
            _lastLoadProblems = string.IsNullOrEmpty(_lastLoadProblems) ? problem : _lastLoadProblems + "; " + problem;
        }

        #endregion
    }
}

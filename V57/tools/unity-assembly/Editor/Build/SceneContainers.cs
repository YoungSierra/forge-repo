using UnityEngine;

namespace V57.Assembly.Build
{
    /// <summary>
    /// The six mandatory root containers (SCENE_PRODUCTION_STANDARDS.md) plus <c>_Gameplay/Spawned</c>
    /// (only runtime-spawn root) and <c>_Environment/_Markers</c> (typed V57Marker objects).
    /// </summary>
    public sealed class SceneContainers
    {
        #region Public Methods

        public Transform Environment { get; private set; }

        public Transform Markers { get; private set; }

        public Transform Gameplay { get; private set; }

        public Transform Spawned { get; private set; }

        public Transform Systems { get; private set; }

        public Transform Ui { get; private set; }

        public Transform Cameras { get; private set; }

        public Transform Lighting { get; private set; }

        /// <summary>Creates the containers in the active scene.</summary>
        public static SceneContainers Create()
        {
            SceneContainers containers = new SceneContainers
            {
                Environment = CreateRoot("_Environment"),
                Gameplay = CreateRoot("_Gameplay"),
                Systems = CreateRoot("_Systems"),
                Ui = CreateRoot("_UI"),
                Cameras = CreateRoot("_Cameras"),
                Lighting = CreateRoot("_Lighting")
            };
            containers.Markers = CreateChild(containers.Environment, "_Markers");
            containers.Spawned = CreateChild(containers.Gameplay, "Spawned");
            return containers;
        }

        public static Transform CreateChild(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        #endregion

        #region Private Methods

        private SceneContainers()
        {
        }

        private static Transform CreateRoot(string name)
        {
            return new GameObject(name).transform;
        }

        #endregion
    }
}

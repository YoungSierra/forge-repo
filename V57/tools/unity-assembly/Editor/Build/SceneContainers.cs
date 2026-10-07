using UnityEngine;
using UnityEngine.SceneManagement;

namespace V57.Assembly.Build
{
    /// <summary>
    /// The six mandatory root containers (SCENE_PRODUCTION_STANDARDS.md) plus <c>_Gameplay/Spawned</c>
    /// (only runtime-spawn root), <c>_Environment/_Markers</c> (typed V57Marker objects) and <c>_Gameplay/Level</c>
    /// (gameplay prefabs placed by the level delivery). Level content = everything under <c>_Environment</c> and
    /// <c>_Gameplay/Level</c>; the rest is the scene's gameplay setup and is never touched by a level rebuild.
    /// </summary>
    public sealed class SceneContainers
    {
        #region Public Methods

        public Transform Environment { get; private set; }

        public Transform Markers { get; private set; }

        public Transform Gameplay { get; private set; }

        public Transform LevelActors { get; private set; }

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
            containers.LevelActors = CreateChild(containers.Gameplay, "Level");
            containers.Spawned = CreateChild(containers.Gameplay, "Spawned");
            return containers;
        }

        /// <summary>Finds the containers of an existing scene, creating any that are missing.</summary>
        public static SceneContainers FromScene(Scene scene)
        {
            SceneContainers containers = new SceneContainers
            {
                Environment = FindOrCreateRoot(scene, "_Environment"),
                Gameplay = FindOrCreateRoot(scene, "_Gameplay"),
                Systems = FindOrCreateRoot(scene, "_Systems"),
                Ui = FindOrCreateRoot(scene, "_UI"),
                Cameras = FindOrCreateRoot(scene, "_Cameras"),
                Lighting = FindOrCreateRoot(scene, "_Lighting")
            };
            containers.Markers = FindOrCreateChild(containers.Environment, "_Markers");
            containers.LevelActors = FindOrCreateChild(containers.Gameplay, "Level");
            containers.Spawned = FindOrCreateChild(containers.Gameplay, "Spawned");
            return containers;
        }

        /// <summary>Removes the level content (all of <c>_Environment</c> and <c>_Gameplay/Level</c>); returns removed objects.</summary>
        public int ClearLevelContent()
        {
            int removed = DestroyChildren(Environment) + DestroyChildren(LevelActors);
            Markers = CreateChild(Environment, "_Markers");
            return removed;
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

        private static Transform FindOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root.transform;
                }
            }

            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created.transform;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child : CreateChild(parent, name);
        }

        private static int DestroyChildren(Transform parent)
        {
            int count = parent.childCount;
            for (int i = count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }

            return count;
        }

        #endregion
    }
}

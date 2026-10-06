using UnityEngine;

namespace V57.GameForge
{
    /// <summary>
    /// Procedural graybox primitives for Unity URP prototypes.
    /// </summary>
    public static class GameForgePrimitives
    {
        public static GameObject MakeBox(string name, Vector3 size, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = size;
            ApplyColor(go, color);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject MakeSphere(string name, float radius, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = Vector3.one * (radius * 2f);
            ApplyColor(go, color);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject MakeCapsule(string name, float height, float radius, Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            ApplyColor(go, color);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject MakeCharacter(string name, Color color, Transform parent = null)
        {
            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            var body = MakeCapsule("Body", 1.6f, 0.35f, color, root.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            return root;
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            if (shader == null) return;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            renderer.sharedMaterial = mat;
        }
    }
}

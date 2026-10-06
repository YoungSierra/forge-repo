using System;
using UnityEngine;
using V57.Assembly.Data;
using V57.Assembly.Report;

namespace V57.Assembly.Build
{
    /// <summary>
    /// Cameras from <c>Marker_Camera_&lt;ViewId&gt;</c> poses with projection values from <c>camera.json</c>.
    /// One camera is main (camera_ref match, else the first): tagged MainCamera, AudioListener, enabled; others disabled.
    /// </summary>
    public static class CameraPlacement
    {
        #region Public Methods

        public static Camera Create(Transform camerasRoot, Transform pose, string viewId, string sceneId)
        {
            GameObject cameraObject = new GameObject("CAM_" + viewId);
            cameraObject.transform.SetParent(camerasRoot, false);
            cameraObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
            Camera camera = cameraObject.AddComponent<Camera>();
            ApplyView(FindView(viewId, sceneId), camera);
            return camera;
        }

        /// <summary>Picks the main camera; creates one from camera.json (angle/distance) when the blockout has none.</summary>
        public static void AssignMain(Transform camerasRoot, SceneEntryDto scene)
        {
            Camera[] cameras = camerasRoot.GetComponentsInChildren<Camera>(true);
            if (cameras.Length == 0)
            {
                cameras = new[] { CreateDefault(camerasRoot, scene) };
            }

            Camera main = cameras[0];
            string reference = NormalizeViewId(scene.camera_ref);
            foreach (Camera camera in cameras)
            {
                string viewId = camera.name.StartsWith("CAM_", StringComparison.Ordinal) ? camera.name.Substring(4) : camera.name;
                if (reference.Length > 0 && string.Equals(reference, viewId, StringComparison.OrdinalIgnoreCase))
                {
                    main = camera;
                }
            }

            foreach (Camera camera in cameras)
            {
                bool isMain = camera == main;
                camera.enabled = isMain;
                camera.gameObject.tag = isMain ? "MainCamera" : "Untagged";
                if (isMain && camera.GetComponent<AudioListener>() == null)
                {
                    camera.gameObject.AddComponent<AudioListener>();
                }
            }
        }

        #endregion

        #region Private Methods

        private static Camera CreateDefault(Transform camerasRoot, SceneEntryDto scene)
        {
            CameraViewDto view = FindView(scene.camera_ref, scene.id) ?? FirstViewForScene(scene.id);
            float angle = view != null && view.angle_deg > 0f ? view.angle_deg : 45f;
            float distance = view != null && view.distance_m > 0f ? view.distance_m : 15f;
            AssemblyContext.Warn("BuildLevelScenes", $"{scene.id}: no Marker_Camera_*; Main Camera from camera.json view '{view?.id ?? "none"}' (angle {angle}, distance {distance})");
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(camerasRoot, false);
            Quaternion rotation = Quaternion.Euler(angle, 0f, 0f);
            cameraObject.transform.SetPositionAndRotation(rotation * new Vector3(0f, 0f, -distance), rotation);
            Camera camera = cameraObject.AddComponent<Camera>();
            ApplyView(view, camera);
            return camera;
        }

        private static CameraViewDto FindView(string viewId, string sceneId)
        {
            CameraDto cameraData = GeneratedData.Camera;
            if (cameraData?.views == null || string.IsNullOrEmpty(viewId))
            {
                return null;
            }

            string wanted = NormalizeViewId(viewId);
            foreach (CameraViewDto view in cameraData.views)
            {
                bool idMatches = view != null && string.Equals(NormalizeViewId(view.id), wanted, StringComparison.OrdinalIgnoreCase);
                bool sceneMatches = view != null && (string.IsNullOrEmpty(view.scene) || string.Equals(view.scene, sceneId, StringComparison.OrdinalIgnoreCase));
                if (idMatches && sceneMatches)
                {
                    return view;
                }
            }

            return null;
        }

        private static CameraViewDto FirstViewForScene(string sceneId)
        {
            foreach (CameraViewDto view in GeneratedData.Camera?.views ?? new CameraViewDto[0])
            {
                if (view != null && string.Equals(view.scene, sceneId, StringComparison.OrdinalIgnoreCase))
                {
                    return view;
                }
            }

            return null;
        }

        /// <summary>camera_ref may be a view id, <c>Marker_Camera_&lt;Id&gt;</c> or a Docs/ArtDirection/Camera/&lt;Id&gt;.png path.</summary>
        private static string NormalizeViewId(string reference)
        {
            string stem = System.IO.Path.GetFileNameWithoutExtension((reference ?? string.Empty).Replace('\\', '/'));
            return stem.StartsWith("Marker_Camera_", StringComparison.Ordinal) ? stem.Substring(14) : stem;
        }

        private static void ApplyView(CameraViewDto view, Camera camera)
        {
            if (view == null)
            {
                return;
            }

            camera.orthographic = string.Equals(view.type, "orthographic", StringComparison.OrdinalIgnoreCase);
            if (camera.orthographic && view.fov_or_size > 0f)
            {
                camera.orthographicSize = view.fov_or_size;
            }
            else if (!camera.orthographic && view.fov_or_size > 0f)
            {
                camera.fieldOfView = view.fov_or_size;
            }
        }

        #endregion
    }
}

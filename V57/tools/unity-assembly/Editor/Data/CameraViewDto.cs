using System;

namespace V57.Assembly.Data
{
    /// <summary>
    /// One <c>camera.views[]</c> entry. <c>follow</c> is mapped as a string (target id or empty).
    /// JSON DTO: public snake_case fields mirror Docs/Generated/json keys (JsonUtility requirement) —
    /// documented exception to STANDARDS_CANONICAL §3, see README "Standards exceptions".
    /// </summary>
    [Serializable]
    public sealed class CameraViewDto
    {
        public string id;
        public string scene;
        public string type;
        public float angle_deg;
        public float fov_or_size;
        public float distance_m;
        public string follow;
        public string notes;
    }
}

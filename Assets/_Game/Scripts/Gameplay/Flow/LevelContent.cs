using System.Collections.Generic;
using System.Text;
using ProfessorSprat.Gameplay.AccessKey;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using UnityEngine;
using V57.GoldPath;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// Level contract (TDD §6) read at Init by traversing the two level containers ZoneFlow references
    /// (<c>_Environment/_Markers</c>, <c>_Gameplay/Level</c>) — never by global lookups, never by references to individual
    /// level objects, so a rebuilt map binds without manual work. Validates the contract and reports violations.
    /// </summary>
    public sealed class LevelContent
    {
        #region Public Methods

        public Transform Spawn { get; private set; }

        public float DefaultYaw { get; private set; }

        public List<ZoneRuntime> Zones { get; } = new List<ZoneRuntime>();

        public List<Collider> KillVolumes { get; } = new List<Collider>();

        public List<Collider> CameraZones { get; } = new List<Collider>();

        public List<float> CameraZoneYaws { get; } = new List<float>();

        public List<string> Problems { get; } = new List<string>();

        public static LevelContent Scan(Transform markersRoot, Transform actorsRoot)
        {
            LevelContent content = new LevelContent();
            Dictionary<string, Transform> patrols = new Dictionary<string, Transform>();
            List<Collider> exits = new List<Collider>();
            List<string> exitIds = new List<string>();
            foreach (V57Marker marker in markersRoot.GetComponentsInChildren<V57Marker>(true))
            {
                content.AddMarker(marker, patrols, exits, exitIds);
            }

            content.Zones.Sort((a, b) => NaturalCompare(a.Id, b.Id));
            for (int i = 0; i < exits.Count; i++)
            {
                ZoneRuntime zone = content.Zones.Find(z => z.Id == exitIds[i]);
                if (zone != null)
                {
                    zone.Exit = exits[i];
                }
            }

            content.AssignActors(actorsRoot, patrols);
            content.Validate();
            return content;
        }

        /// <summary>One-way patrol range: distance from the crab to its <c>Marker_Patrol_&lt;Crab&gt;</c> along its local X (0 = static).</summary>
        public float PatrolRange(CrabController crab)
        {
            return Patrols.TryGetValue(crab.CrabId, out Transform end) ? Mathf.Max(0f, Vector3.Dot(end.position - crab.transform.position, crab.transform.right)) : 0f;
        }

        public Dictionary<string, Transform> Patrols { get; private set; } = new Dictionary<string, Transform>();

        public string Report()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string problem in Problems)
            {
                builder.AppendLine(problem);
            }

            return builder.ToString();
        }

        #endregion

        #region Private Methods

        private void AddMarker(V57Marker marker, Dictionary<string, Transform> patrols, List<Collider> exits, List<string> exitIds)
        {
            Collider volume = marker.GetComponent<Collider>();
            switch (marker.Kind)
            {
                case V57MarkerKind.Spawn when marker.Id == "Player":
                    if (Spawn != null)
                    {
                        Problems.Add("more than one Marker_Spawn_Player (LR-03)");
                    }

                    Spawn = marker.transform;
                    break;
                case V57MarkerKind.Zone:
                    Zones.Add(new ZoneRuntime(marker.Id, volume));
                    break;
                case V57MarkerKind.Exit:
                    exits.Add(volume);
                    exitIds.Add(marker.Id);
                    break;
                case V57MarkerKind.Kill:
                    KillVolumes.Add(volume);
                    break;
                case V57MarkerKind.CameraZone when marker.Id == "Default":
                    DefaultYaw = marker.transform.eulerAngles.y;
                    break;
                case V57MarkerKind.CameraZone:
                    CameraZones.Add(volume);
                    CameraZoneYaws.Add(marker.transform.eulerAngles.y);
                    break;
                case V57MarkerKind.Patrol:
                    patrols[marker.Id] = marker.transform;
                    break;
            }

            Patrols = patrols;
        }

        private void AssignActors(Transform actorsRoot, Dictionary<string, Transform> patrols)
        {
            foreach (FlyController fly in actorsRoot.GetComponentsInChildren<FlyController>(true))
            {
                ZoneFor(fly.transform.position, fly.name)?.Flies.Add(fly);
            }

            foreach (CrabController crab in actorsRoot.GetComponentsInChildren<CrabController>(true))
            {
                ZoneFor(crab.transform.position, crab.name)?.Crabs.Add(crab);
            }

            foreach (AccessKeyController key in actorsRoot.GetComponentsInChildren<AccessKeyController>(true))
            {
                ZoneRuntime zone = ZoneFor(key.transform.position, key.name);
                if (zone != null)
                {
                    zone.Key = zone.Key == null ? key : zone.Key;
                }
            }

            foreach (ZoneDoorController door in actorsRoot.GetComponentsInChildren<ZoneDoorController>(true))
            {
                ZoneRuntime zone = ZoneFor(door.transform.position, door.name);
                if (zone != null)
                {
                    zone.Door = zone.Door == null ? door : zone.Door;
                }
            }
        }

        private ZoneRuntime ZoneFor(Vector3 position, string actor)
        {
            foreach (ZoneRuntime zone in Zones)
            {
                if (zone.Contains(position))
                {
                    return zone;
                }
            }

            Problems.Add($"{actor} is outside every Marker_Zone volume; ignored");
            return null;
        }

        private void Validate()
        {
            if (Spawn == null)
            {
                Problems.Add("no Marker_Spawn_Player (LR-03)");
            }

            if (Zones.Count == 0)
            {
                Problems.Add("no Marker_Zone_<ZoneId> (level contract)");
            }

            foreach (ZoneRuntime zone in Zones)
            {
                if (zone.Flies.Count < 1 || zone.Flies.Count > 30 || zone.Key == null || zone.Door == null || zone.Exit == null)
                {
                    Problems.Add($"zone {zone.Id}: flies {zone.Flies.Count} (1–30), key {(zone.Key != null)}, door {(zone.Door != null)}, exit {(zone.Exit != null)} (LR-03)");
                }
            }
        }

        private static int NaturalCompare(string a, string b)
        {
            return a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
        }

        #endregion
    }
}

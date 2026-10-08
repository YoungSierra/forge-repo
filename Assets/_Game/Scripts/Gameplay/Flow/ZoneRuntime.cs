using System.Collections.Generic;
using ProfessorSprat.Gameplay.AccessKey;
using ProfessorSprat.Gameplay.Crab;
using ProfessorSprat.Gameplay.Flies;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>One zone of the level, assembled at Init from the level contract (TDD §6): its volume, exit, and the
    /// flies, crabs, key and door placed inside the volume.</summary>
    public sealed class ZoneRuntime
    {
        #region Public Methods

        public ZoneRuntime(string id, Collider volume)
        {
            Id = id;
            Volume = volume;
        }

        public string Id { get; }

        public Collider Volume { get; }

        public Collider Exit { get; set; }

        public List<FlyController> Flies { get; } = new List<FlyController>();

        public List<CrabController> Crabs { get; } = new List<CrabController>();

        public AccessKeyController Key { get; set; }

        public ZoneDoorController Door { get; set; }

        public bool Contains(Vector3 point)
        {
            return Contains(Volume, point);
        }

        public static bool Contains(Collider volume, Vector3 point)
        {
            return volume != null && (volume.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
        }

        #endregion
    }
}

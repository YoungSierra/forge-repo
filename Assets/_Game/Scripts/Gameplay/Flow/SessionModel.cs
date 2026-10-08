using System;
using System.Collections.Generic;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>One cleared zone (TDD §11.4).</summary>
    [Serializable]
    public sealed class ZoneResult
    {
        public string levelId;
        public string zoneId;
        public int fliesCollected;
        public int fliesTotal;
        public float bestTimeSeconds;
    }

    /// <summary>
    /// Persistent session data (TDD §11.4): only completed zones, keyed by level/zone id — no count is pinned, so new zones
    /// or levels never change the schema. JSON DTO: snake-free public fields required by JsonUtility.
    /// </summary>
    [Serializable]
    public sealed class SessionModel
    {
        public int save_schema_version = SaveSchema.CurrentVersion;
        public string lastLevelId;
        public List<ZoneResult> completedZones = new List<ZoneResult>();

        /// <summary>Records a cleared zone, keeping the best time.</summary>
        public void Record(string levelId, string zoneId, int fliesCollected, int fliesTotal, float seconds)
        {
            lastLevelId = levelId;
            ZoneResult existing = completedZones.Find(z => z.levelId == levelId && z.zoneId == zoneId);
            if (existing == null)
            {
                completedZones.Add(new ZoneResult { levelId = levelId, zoneId = zoneId, fliesCollected = fliesCollected, fliesTotal = fliesTotal, bestTimeSeconds = seconds });
                return;
            }

            existing.fliesCollected = Math.Max(existing.fliesCollected, fliesCollected);
            existing.fliesTotal = fliesTotal;
            existing.bestTimeSeconds = existing.bestTimeSeconds > 0f ? Math.Min(existing.bestTimeSeconds, seconds) : seconds;
        }
    }

    /// <summary>Save schema version and migration (TDD §11.4).</summary>
    public static class SaveSchema
    {
        public const int CurrentVersion = 1;

        /// <summary>Upgrades an older snapshot in place before use; v1 is the first version.</summary>
        public static SessionModel Migrate(SessionModel loaded)
        {
            if (loaded == null)
            {
                return new SessionModel();
            }

            loaded.completedZones ??= new List<ZoneResult>();
            loaded.save_schema_version = CurrentVersion;
            return loaded;
        }
    }
}

using System.Collections.Generic;

namespace ProfessorSprat.Gameplay.Flies
{
    /// <summary>
    /// Plain runtime state of one zone run: registered fly ids, collected ids, completion. The total is the number of flies
    /// registered before the zone starts (flies placed in the zone); later registrations are rejected. Never decrements.
    /// </summary>
    public sealed class ZoneFlyModel
    {
        #region Fields

        private readonly HashSet<string> _registered = new HashSet<string>();
        private readonly HashSet<string> _collected = new HashSet<string>();

        #endregion

        #region Public Methods

        public string ZoneId { get; private set; }

        public bool Started { get; private set; }

        public bool IsComplete { get; private set; }

        public int ZoneTotal => _registered.Count;

        public int CollectedCount => _collected.Count;

        /// <summary>False when the zone already started (registration closed) or the id is a duplicate.</summary>
        public bool Register(string flyId)
        {
            return !Started && _registered.Add(flyId);
        }

        public void Begin(string zoneId)
        {
            ZoneId = zoneId;
            Started = true;
        }

        /// <summary>Returns true when the fly counted (registered, not yet collected, zone not complete).</summary>
        public bool Collect(string flyId)
        {
            if (!Started || IsComplete || !_registered.Contains(flyId) || !_collected.Add(flyId))
            {
                return false;
            }

            IsComplete = _collected.Count == _registered.Count;
            return true;
        }

        #endregion
    }
}

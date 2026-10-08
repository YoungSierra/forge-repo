using UnityEngine;

namespace ProfessorSprat.Core
{
    /// <summary>TDD OnFlyCollected.</summary>
    public readonly struct FlyCollectedEvent
    {
        public FlyCollectedEvent(string flyId, string zoneId, int newCount, int zoneTotal)
        {
            FlyId = flyId;
            ZoneId = zoneId;
            NewCount = newCount;
            ZoneTotal = zoneTotal;
        }

        public string FlyId { get; }

        public string ZoneId { get; }

        public int NewCount { get; }

        public int ZoneTotal { get; }
    }

    /// <summary>TDD OnZoneComplete (all flies of the zone collected).</summary>
    public readonly struct ZoneCompletedEvent
    {
        public ZoneCompletedEvent(string zoneId)
        {
            ZoneId = zoneId;
        }

        public string ZoneId { get; }
    }

    /// <summary>TDD OnKeySpawned.</summary>
    public readonly struct KeySpawnedEvent
    {
        public KeySpawnedEvent(string zoneId, Vector3 keyPosition, Transform key)
        {
            ZoneId = zoneId;
            KeyPosition = keyPosition;
            Key = key;
        }

        public string ZoneId { get; }

        public Vector3 KeyPosition { get; }

        public Transform Key { get; }
    }

    /// <summary>TDD OnKeyCollected.</summary>
    public readonly struct KeyCollectedEvent
    {
        public KeyCollectedEvent(string zoneId)
        {
            ZoneId = zoneId;
        }

        public string ZoneId { get; }
    }

    /// <summary>TDD OnDoorUnlocked.</summary>
    public readonly struct DoorUnlockedEvent
    {
        public DoorUnlockedEvent(string zoneId, string doorId)
        {
            ZoneId = zoneId;
            DoorId = doorId;
        }

        public string ZoneId { get; }

        public string DoorId { get; }
    }

    /// <summary>TDD OnCrabContactPlayer.</summary>
    public readonly struct CrabContactPlayerEvent
    {
        public CrabContactPlayerEvent(string crabId, Vector3 knockbackDirection)
        {
            CrabId = crabId;
            KnockbackDirection = knockbackDirection;
        }

        public string CrabId { get; }

        public Vector3 KnockbackDirection { get; }
    }

    /// <summary>TDD OnCrabDefeated.</summary>
    public readonly struct CrabDefeatedEvent
    {
        public CrabDefeatedEvent(string crabId, Vector3 position)
        {
            CrabId = crabId;
            Position = position;
        }

        public string CrabId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>TDD OnCrabProximity (near = within the warn radius, with hysteresis).</summary>
    public readonly struct CrabProximityEvent
    {
        public CrabProximityEvent(string crabId, bool near, Vector3 position)
        {
            CrabId = crabId;
            Near = near;
            Position = position;
        }

        public string CrabId { get; }

        public bool Near { get; }

        public Vector3 Position { get; }
    }

    /// <summary>TDD OnZoneStarted.</summary>
    public readonly struct ZoneStartedEvent
    {
        public ZoneStartedEvent(string zoneId, int zoneTotal)
        {
            ZoneId = zoneId;
            ZoneTotal = zoneTotal;
        }

        public string ZoneId { get; }

        public int ZoneTotal { get; }
    }

    /// <summary>TDD OnZoneCleared (zone exit reached after the door opened).</summary>
    public readonly struct ZoneClearedEvent
    {
        public ZoneClearedEvent(string levelId, string zoneId, int fliesCollected, int fliesTotal, float elapsedSeconds, bool isLastZone)
        {
            LevelId = levelId;
            ZoneId = zoneId;
            FliesCollected = fliesCollected;
            FliesTotal = fliesTotal;
            ElapsedSeconds = elapsedSeconds;
            IsLastZone = isLastZone;
        }

        public string LevelId { get; }

        public string ZoneId { get; }

        public int FliesCollected { get; }

        public int FliesTotal { get; }

        public float ElapsedSeconds { get; }

        public bool IsLastZone { get; }
    }
}

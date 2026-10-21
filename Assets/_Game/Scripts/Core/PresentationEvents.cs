using UnityEngine;

namespace ProfessorSprat.Core
{
    /// <summary>Who speaks a bark: the speech bubble anchors over that character.</summary>
    public enum Speaker
    {
        Professor,
        Sprat
    }

    /// <summary>Full-screen flash tint (D-287).</summary>
    public enum FlashKind
    {
        White,
        Blue
    }

    /// <summary>Centre-screen banner: a title with a small subtitle (D-287).</summary>
    public readonly struct BannerEvent
    {
        public BannerEvent(string title, string subtitle = "")
        {
            Title = title;
            Subtitle = subtitle;
        }

        public string Title { get; }

        public string Subtitle { get; }
    }

    /// <summary>A short line in a speech bubble over the Professor or Sprat (D-287).</summary>
    public readonly struct BarkEvent
    {
        public BarkEvent(Speaker speaker, string text, float duration = 3.2f)
        {
            Speaker = speaker;
            Text = text;
            Duration = duration;
        }

        public Speaker Speaker { get; }

        public string Text { get; }

        public float Duration { get; }
    }

    /// <summary>Full-screen flash that fades out (D-287). Peak is the starting opacity.</summary>
    public readonly struct ScreenFlashEvent
    {
        public ScreenFlashEvent(FlashKind kind, float peak, float duration)
        {
            Kind = kind;
            Peak = peak;
            Duration = duration;
        }

        public FlashKind Kind { get; }

        public float Peak { get; }

        public float Duration { get; }
    }

    /// <summary>Camera shake request; strength is the impulse amplitude in metres (D-287).</summary>
    public readonly struct CameraShakeEvent
    {
        public CameraShakeEvent(float strength)
        {
            Strength = strength;
        }

        public float Strength { get; }
    }

    /// <summary>The Professor reached a zone door without its key (TDD §B KeyMaterialisation "locked clunk").</summary>
    public readonly struct DoorLockedEvent
    {
        public DoorLockedEvent(string zoneId, Vector3 position)
        {
            ZoneId = zoneId;
            Position = position;
        }

        public string ZoneId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>The key reached its door and was socketed (it then shrinks away).</summary>
    public readonly struct KeySocketedEvent
    {
        public KeySocketedEvent(string zoneId)
        {
            ZoneId = zoneId;
        }

        public string ZoneId { get; }
    }
}

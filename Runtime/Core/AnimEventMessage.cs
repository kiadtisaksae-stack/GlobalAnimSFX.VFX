using UnityEngine;

namespace GlobalAnimEvents.Core
{
    public sealed class AnimEventMessage
    {
        public AnimEventType Type { get; internal set; }
        public GameObject Owner { get; internal set; }
        public Animator Animator { get; internal set; }
        public AnimationClip Clip { get; internal set; }
        public float Time { get; internal set; }
        public float NormalizedTime { get; internal set; }
        public string SocketName { get; internal set; }
        public GameObject Prefab { get; internal set; }
        public AudioClip AudioClip { get; internal set; }
        public AnimEventPayload Payload { get; internal set; }
        public AnimEventMarker Marker { get; internal set; }

        public static AnimEventMessageBuilder For(AnimEventType type)
        {
            return new AnimEventMessageBuilder(type);
        }
    }
}

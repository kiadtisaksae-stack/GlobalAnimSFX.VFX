using System;
using UnityEngine;

namespace GlobalAnimEvents.Core
{
    [Serializable]
    public sealed class AnimEventMarker
    {
        public int markerId;
        public string eventId = "Event";
        public AnimEventType type = AnimEventType.Vfx;
        public float time;
        public float normalizedTime;
        public string socketName;
        public GameObject prefab;
        public AudioClip audioClip;
        public Vector3 localPositionOffset = Vector3.zero;
        public Vector3 localEulerOffset = Vector3.zero;
        public Vector3 localScale = Vector3.one;
        public float floatValue;
        public int intValue;
        public string stringValue;
        public Vector3 vectorValue;
        public AnimEventPayload payload = new AnimEventPayload();
    }
}

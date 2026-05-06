using System;
using UnityEngine;

namespace GlobalAnimEvents.Core
{
    [Serializable]
    public sealed class AnimEventPayload
    {
        public float floatValue;
        public int intValue;
        public string stringValue;
        public Vector3 vectorValue;
    }
}

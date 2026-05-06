using System.Collections.Generic;
using UnityEngine;

namespace GlobalAnimEvents.Core
{
    [CreateAssetMenu(menuName = "Animation/Anim Event Track", fileName = "AnimEventTrack")]
    public sealed class AnimEventTrackAsset : ScriptableObject
    {
        public AnimationClip animationClip;
        public List<AnimEventMarker> events = new List<AnimEventMarker>();
    }
}

using System.Collections.Generic;
using GlobalAnimEvents.Core;
using GlobalAnimEvents.Dispatcher;
using UnityEngine;

namespace GlobalAnimEvents.Player
{
    public sealed class AnimEventPlayer : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private GlobalAnimEventDispatcher dispatcher;
        [SerializeField] private List<AnimEventTrackAsset> trackAssets = new List<AnimEventTrackAsset>();

        private readonly Dictionary<AnimationClip, AnimEventTrackAsset> _clipToTrack = new Dictionary<AnimationClip, AnimEventTrackAsset>();
        private readonly Dictionary<AnimEventTrackAsset, Dictionary<int, AnimEventMarker>> _trackMarkers = new Dictionary<AnimEventTrackAsset, Dictionary<int, AnimEventMarker>>();

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (dispatcher == null) dispatcher = GetComponent<GlobalAnimEventDispatcher>();
            RebuildCache();
        }

        public void RebuildCache()
        {
            _clipToTrack.Clear();
            _trackMarkers.Clear();

            for (int i = 0; i < trackAssets.Count; i++)
            {
                AnimEventTrackAsset track = trackAssets[i];
                if (track == null || track.animationClip == null)
                {
                    continue;
                }

                _clipToTrack[track.animationClip] = track;
                Dictionary<int, AnimEventMarker> markerMap = new Dictionary<int, AnimEventMarker>();
                for (int m = 0; m < track.events.Count; m++)
                {
                    AnimEventMarker marker = track.events[m];
                    if (marker != null)
                    {
                        markerMap[marker.markerId] = marker;
                    }
                }

                _trackMarkers[track] = markerMap;
            }
        }

        public void AnimEvent_Emit(AnimationEvent evt)
        {
            if (evt == null || dispatcher == null || animator == null)
            {
                return;
            }

            AnimationClip clip = ResolveCurrentClip(evt);
            if (clip == null || !_clipToTrack.TryGetValue(clip, out AnimEventTrackAsset track))
            {
                return;
            }

            if (!_trackMarkers.TryGetValue(track, out Dictionary<int, AnimEventMarker> markerMap))
            {
                return;
            }

            if (!markerMap.TryGetValue(evt.intParameter, out AnimEventMarker marker) || marker == null)
            {
                return;
            }

            AnimEventMessage message = AnimEventMessage.For(marker.type)
                .FromOwner(gameObject)
                .FromAnimator(animator)
                .FromClip(track.animationClip)
                .AtTime(marker.time)
                .AtNormalizedTime(marker.normalizedTime)
                .AtSocket(marker.socketName)
                .WithPrefab(marker.prefab)
                .WithAudio(marker.audioClip)
                .WithPayload(marker.payload)
                .WithMarker(marker)
                .Build();

            dispatcher.Dispatch(message);
        }

        private static AnimationClip ResolveCurrentClip(AnimationEvent evt)
        {
            if (evt.animatorClipInfo.weight > 0f)
            {
                return evt.animatorClipInfo.clip;
            }

            return evt.animatorStateInfo.length > 0f && evt.animatorClipInfo.clip != null
                ? evt.animatorClipInfo.clip
                : evt.animatorClipInfo.clip;
        }
    }
}

using UnityEngine;

namespace GlobalAnimEvents.Core
{
    public sealed class AnimEventMessageBuilder
    {
        private readonly AnimEventMessage _message;

        internal AnimEventMessageBuilder(AnimEventType type)
        {
            _message = new AnimEventMessage
            {
                Type = type
            };
        }

        public AnimEventMessageBuilder FromOwner(GameObject owner)
        {
            _message.Owner = owner;
            return this;
        }

        public AnimEventMessageBuilder FromAnimator(Animator animator)
        {
            _message.Animator = animator;
            return this;
        }

        public AnimEventMessageBuilder FromClip(AnimationClip clip)
        {
            _message.Clip = clip;
            return this;
        }

        public AnimEventMessageBuilder AtTime(float t)
        {
            _message.Time = t;
            return this;
        }

        public AnimEventMessageBuilder AtNormalizedTime(float t)
        {
            _message.NormalizedTime = t;
            return this;
        }

        public AnimEventMessageBuilder AtSocket(string socket)
        {
            _message.SocketName = socket;
            return this;
        }

        public AnimEventMessageBuilder WithPrefab(GameObject prefab)
        {
            _message.Prefab = prefab;
            return this;
        }

        public AnimEventMessageBuilder WithAudio(AudioClip clip)
        {
            _message.AudioClip = clip;
            return this;
        }

        public AnimEventMessageBuilder WithPayload(AnimEventPayload payload)
        {
            _message.Payload = payload;
            return this;
        }

        public AnimEventMessageBuilder WithMarker(AnimEventMarker marker)
        {
            _message.Marker = marker;
            return this;
        }

        public AnimEventMessage Build()
        {
            return _message;
        }
    }
}

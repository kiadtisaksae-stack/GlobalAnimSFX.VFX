using GlobalAnimEvents.Core;
using GlobalAnimEvents.Dispatcher;
using UnityEngine;

namespace GlobalAnimEvents.Handlers
{
    public sealed class SfxAnimEventHandler : MonoBehaviour, IAnimEventHandler
    {
        public bool CanHandle(AnimEventMessage message)
        {
            return message.Type == AnimEventType.Sfx;
        }

        public void Handle(AnimEventMessage message)
        {
            if (message?.AudioClip == null || message.Animator == null)
            {
                return;
            }

            Transform anchor = ResolveAnchor(message.Animator.transform, message.SocketName);
            float volume = message.Marker != null && message.Marker.floatValue > 0f ? message.Marker.floatValue : 1f;
            AudioSource.PlayClipAtPoint(message.AudioClip, anchor.position, Mathf.Clamp01(volume));
        }

        private static Transform ResolveAnchor(Transform root, string socketName)
        {
            if (root == null || string.IsNullOrWhiteSpace(socketName))
            {
                return root;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == socketName)
                {
                    return all[i];
                }
            }

            return root;
        }
    }
}

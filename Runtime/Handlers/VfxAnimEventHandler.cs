using GlobalAnimEvents.Core;
using GlobalAnimEvents.Dispatcher;
using UnityEngine;

namespace GlobalAnimEvents.Handlers
{
    public sealed class VfxAnimEventHandler : MonoBehaviour, IAnimEventHandler
    {
        public bool CanHandle(AnimEventMessage message)
        {
            return message.Type == AnimEventType.Vfx;
        }

        public void Handle(AnimEventMessage message)
        {
            if (message?.Prefab == null || message.Animator == null)
            {
                return;
            }

            Transform anchor = ResolveAnchor(message.Animator.transform, message.SocketName);
            GameObject instance = Instantiate(message.Prefab);
            Vector3 pos = message.Marker != null ? message.Marker.localPositionOffset : Vector3.zero;
            Vector3 rot = message.Marker != null ? message.Marker.localEulerOffset : Vector3.zero;
            Vector3 scale = message.Marker != null ? message.Marker.localScale : Vector3.one;
            if (scale == Vector3.zero) scale = Vector3.one;

            instance.transform.position = anchor.TransformPoint(pos);
            instance.transform.rotation = anchor.rotation * Quaternion.Euler(rot);
            instance.transform.localScale = scale;

            bool attach = message.Marker != null ? message.Marker.intValue != 0 : false;
            if (attach)
            {
                instance.transform.SetParent(anchor, true);
            }

            float life = message.Marker != null && message.Marker.floatValue > 0f ? message.Marker.floatValue : 3f;
            Destroy(instance, life);
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

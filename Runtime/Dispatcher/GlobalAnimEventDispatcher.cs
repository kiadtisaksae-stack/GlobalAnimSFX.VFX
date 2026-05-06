using System.Collections.Generic;
using GlobalAnimEvents.Core;
using UnityEngine;

namespace GlobalAnimEvents.Dispatcher
{
    public sealed class GlobalAnimEventDispatcher : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] handlerBehaviours;
        private readonly List<IAnimEventHandler> _handlers = new List<IAnimEventHandler>();

        private void Awake()
        {
            RebuildHandlers();
        }

        private void OnValidate()
        {
            RebuildHandlers();
        }

        public void Dispatch(AnimEventMessage message)
        {
            if (message == null)
            {
                return;
            }

            for (int i = 0; i < _handlers.Count; i++)
            {
                IAnimEventHandler handler = _handlers[i];
                if (handler != null && handler.CanHandle(message))
                {
                    handler.Handle(message);
                }
            }
        }

        private void RebuildHandlers()
        {
            _handlers.Clear();
            if (handlerBehaviours == null)
            {
                return;
            }

            for (int i = 0; i < handlerBehaviours.Length; i++)
            {
                if (handlerBehaviours[i] is IAnimEventHandler h)
                {
                    _handlers.Add(h);
                }
            }
        }
    }
}

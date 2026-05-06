using GlobalAnimEvents.Core;

namespace GlobalAnimEvents.Dispatcher
{
    public interface IAnimEventHandler
    {
        bool CanHandle(AnimEventMessage message);
        void Handle(AnimEventMessage message);
    }
}

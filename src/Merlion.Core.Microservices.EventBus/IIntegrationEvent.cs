using System;

namespace Merlion.Core.Microservices.EventBus
{
    public interface IIntegrationEvent
    {
        public IIntegrationEventHeader Header { get; }

        public string TopicName { get; }

        public string Body { get; }

        public DateTime CreatedTime { get; }
    }
}

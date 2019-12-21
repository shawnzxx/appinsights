using System;

namespace Merlion.Core.Microservices.EventBus.Kafka
{
    public class KafkaMessage : IIntegrationEvent
    {
        public IIntegrationEventHeader Header { get; }

        public string TopicName { get; }

        public string Body { get; }

        public DateTime CreatedTime { get; }

        public KafkaMessage(IIntegrationEventHeader header, string topicName, string body)
        {
            Header = header;
            CreatedTime = DateTime.UtcNow;
            TopicName = topicName;
            Body = body;
        }
    }
}

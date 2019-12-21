using System;

namespace Merlion.Core.Microservices.EventBus.Kafka
{
    public class KafkaMessageHeader : IIntegrationEventHeader
    {
        public string OperationId { get; }

        public string OperationParentId { get; }

        public KafkaMessageHeader(string operationId, string operationParentId)
        {
            const string InvalidOperationIdMessage = "Invalid OperationId:'{0}'.";

            if (string.IsNullOrWhiteSpace(operationId))
            {
                throw new ArgumentException(string.Format(InvalidOperationIdMessage, operationId));
            }

            OperationId = operationId;
            OperationParentId = operationParentId;
        }
    }
}

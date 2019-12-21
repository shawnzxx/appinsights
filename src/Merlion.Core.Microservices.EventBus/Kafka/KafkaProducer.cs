using Confluent.Kafka;
using Merlion.Core.Microservices.EventBus.Kafka.AppSettings;
using Merlion.Core.Microservices.EventBus.Kafka.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Merlion.Core.Microservices.EventBus.Kafka
{
    public class KafkaProducer : IProducer
    {
        private readonly ILogger _logger;
        private readonly IProducer<byte[], byte[]> _producer;

        public KafkaProducer(ILogger<KafkaProducer> logger, IOptions<KafkaConfiguration> kafkaSettings)
        {
            _logger = logger;
            _producer = new ProducerBuilder<byte[], byte[]>(kafkaSettings.Value.GetProducerConfig()).Build();
        }

        public async Task ProduceAsync(IIntegrationEvent kafkaMessage)
        {
            try
            {
                var message = new Message<byte[], byte[]>
                {
                    Headers = CreateHeaders(kafkaMessage.Header),
                    Key = Guid.NewGuid().ToByteArray(),//TODO: Review Key Usage during AppInsights development
                    Value = Encoding.UTF8.GetBytes(kafkaMessage.Body),
                    Timestamp = new Timestamp(DateTime.UtcNow, TimestampType.CreateTime),
                };

                var deliveryReport = await _producer.ProduceAsync(kafkaMessage.TopicName, message);

                LogDeliveryReport(deliveryReport, message.Timestamp);
            }
            catch (KafkaException ex)
            {
                if (ex.Error.IsFatal)
                {
                    _logger.LogCritical(ex, ex.Message);
                }

                throw ex;
            }
        }

        private Headers CreateHeaders(IIntegrationEventHeader header)
        {
            return new Headers()
            {
                new Header(HeaderProperty.OperationId, Encoding.UTF8.GetBytes(header.OperationId)),
                new Header(HeaderProperty.Operation_ParentId, Encoding.UTF8.GetBytes(header.OperationParentId))
            };
        }

        private void LogDeliveryReport(DeliveryResult<byte[], byte[]> deliveryReport, Timestamp sendingTime)
        {
            const string DeliveryMessage = @"DeliveryReport => Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Timestamp(UTC): {DeliveryTime}, Latency: {Latency} ms, EventMessage: {Message}";

            var latency = (DateTime.UtcNow - sendingTime.UtcDateTime).TotalMilliseconds;
            var message = Encoding.UTF8.GetString(deliveryReport.Message.Value);

            _logger.LogInformation(DeliveryMessage
                                    , deliveryReport.Topic
                                    , deliveryReport.Partition.Value
                                    , deliveryReport.Offset.Value
                                    , deliveryReport.Timestamp.UtcDateTime
                                    , latency
                                    , message);
        }
    }
}

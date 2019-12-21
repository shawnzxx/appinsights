using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Confluent.Kafka;
using Merlion.Core.Microservices.EventBus.Kafka.AppSettings;
using Merlion.Core.Microservices.EventBus.Kafka.Constants;

namespace Merlion.Core.Microservices.EventBus.Kafka
{
    public class KafkaConsumer : IConsumer
    {
        public event Func<IIntegrationEvent, Task>? OnMessageReceived;

        #region Private fields
        private IConsumer<byte[], byte[]> _consumer;
        private readonly int? _pollInterval;
        private readonly ILogger _logger;
        private readonly KafkaConfiguration _kafkaConfig;
        private readonly string[] _topicArray;
        private readonly string _topics;
        #endregion

        public KafkaConsumer(IOptions<KafkaConfiguration> kafkaOptions, ILogger<KafkaConsumer> logger)
        {
            const string InvalidTopicsErrorMessage = "Invalid Topics in configuration.";

            _logger = logger;
            _kafkaConfig = kafkaOptions.Value;
            _pollInterval = _kafkaConfig.Consumer?.PollIntervalMilliseconds;

            if (_kafkaConfig.Consumer?.Topics is null
                || string.IsNullOrWhiteSpace(_kafkaConfig.Consumer?.Topics))
            {
                throw new Exception(InvalidTopicsErrorMessage);
            }

            _topics = _kafkaConfig.Consumer?.Topics!;
            _topicArray = _topics.Split(',');

            _consumer = BuildConsumer();
        }

        public async Task StartConsumerAsync(CancellationToken cancellationToken)
        {
            const string SubscribeTopicsMessage = "Subscribe to topics: {Topics}";
            const string SubscribeTopicsErrorMessage = "Fail to subscribe to topics: {Topics}. {Message}";
            const string KafkaConsumerCriticalMessage = "Kafka consumer is in an un-recoverable state.";
            const string KafkaConsumerErrorMessage = "Kafka consumer transient error.";
            const string SystemExceptionMessage = "System Exception: {Message}";

            try
            {
                _consumer.Subscribe(_topicArray);
                _logger.LogInformation(SubscribeTopicsMessage, _topics);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, SubscribeTopicsErrorMessage, _topics, ex.Message);
                throw ex;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _pollInterval is null ? _consumer.Consume()
                                                                : _consumer.Consume(TimeSpan.FromMilliseconds(_pollInterval.Value));

                    if (consumeResult == null)
                    {
                        continue;
                    }

                    LogConsumeResult(consumeResult);

                    await ProcessConsumeResultAsync(consumeResult);
                }
                catch (KafkaException ex)
                {
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical(ex, KafkaConsumerCriticalMessage);
                    }
                    else
                    {
                        _logger.LogWarning(ex, KafkaConsumerErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, SystemExceptionMessage, ex.Message);
                }
            }
        }

        public void StopConsumer()
        {
            const string StopConsumerMessage = "{StopConsumer} Stop Consumer invoked.";

            _logger.LogInformation(StopConsumerMessage, nameof(StopConsumer));

            _consumer.Unassign();
        }

        private IConsumer<byte[], byte[]> BuildConsumer()
        {
            const string KafkaErrorMessage = "KafkaError => Code: {KafkaErrorCode}, Reason: {KafkaErrorReason}, Error: {KafkaError}";
            const string KafkaStatisticsMessage = "KafkaStatistics => Statistics: {Statistics}";
            const string KafkaPartitionsAssignedMessage = "PartitionsAssigned => Topic: {Topic}, Partition: {Partition}";
            const string KafkaPartitionsRevokedMessage = "KafkaPartitionsRevoked => Topic: {Topic}, Partition: {Partition}";
            const string KafkaLogMessage = "KafkaLog => Message: {LogMessage}";
            const string KafkaOffsetsCommittedMessage = "OffsetsCommitted => Topic: {Topic}, Partition: {Partition}, Offset: {Offset}";
            const string KafkaOffsetsCommittedErrorMessage = "OffsetsCommitted Error: {OffsetsCommittedError}";

            return new ConsumerBuilder<byte[], byte[]>(_kafkaConfig.GetConsumerConfig())
                .SetErrorHandler((_, error) =>
                {
                    _logger.LogError(KafkaErrorMessage, error.Code.ToString(), error.Reason, error.ToString());
                })
                .SetStatisticsHandler((_, statistics) =>
                {
                    _logger.LogInformation(KafkaStatisticsMessage, statistics);
                })
                .SetPartitionsAssignedHandler((_, topicPartitions) =>
                {
                    topicPartitions.ForEach(tp =>
                    {
                        _logger.LogInformation(KafkaPartitionsAssignedMessage, tp.Topic, tp.Partition.Value);
                    });
                })
                .SetPartitionsRevokedHandler((_, topicPartitionOffsets) =>
                {
                    topicPartitionOffsets.ForEach(partition =>
                    {
                        _logger.LogWarning(KafkaPartitionsRevokedMessage, partition.Topic, partition.Partition.Value);
                    });
                })
                .SetLogHandler((_, logMsg) =>
                {
                    #region SetLogHandler
                    switch (logMsg.Level)
                    {
                        case SyslogLevel.Emergency:
                        case SyslogLevel.Critical:
                        case SyslogLevel.Alert:
                            _logger.LogCritical(KafkaLogMessage, logMsg);
                            break;
                        case SyslogLevel.Error:
                            _logger.LogError(KafkaLogMessage, logMsg);
                            break;
                        case SyslogLevel.Warning:
                            _logger.LogWarning(KafkaLogMessage, logMsg);
                            break;
                        case SyslogLevel.Notice:
                        case SyslogLevel.Info:
                            _logger.LogInformation(KafkaLogMessage, logMsg);
                            break;
                        case SyslogLevel.Debug:
                            _logger.LogDebug(KafkaLogMessage, logMsg);
                            break;
                        default:
                            _logger.LogInformation(KafkaLogMessage, logMsg);
                            break;
                    }

                    #endregion
                })
                .SetOffsetsCommittedHandler((_, committedOffsets) =>
                {
                    #region SetOffsetsCommittedHandler
                    foreach (var offset in committedOffsets.Offsets)
                    {
                        if (offset.Error.Code == ErrorCode.NoError)
                        {
                            _logger.LogInformation(KafkaOffsetsCommittedMessage, offset.Topic, offset.Partition.Value, offset.Offset.Value);
                        }
                        else
                        {
                            _logger.LogError(KafkaOffsetsCommittedErrorMessage, offset.Error);
                        }
                    }
                    #endregion
                })
                .Build();
        }

        private async Task ProcessConsumeResultAsync(ConsumeResult<byte[], byte[]> consumeResult)
        {
            const string StartProcessMessage = "Start Process Event {Topic}. partition {Partition} offset {Offset} key {Key}";
            const string OnMessageReceivedExceptionMessage = "OnMessageReceivedException => {kafkaMessage}";

            var operationId = Encoding.UTF8.GetString(consumeResult.Headers.GetLastBytes(HeaderProperty.OperationId));
            var operationParentId = Encoding.UTF8.GetString(consumeResult.Headers.GetLastBytes(HeaderProperty.Operation_ParentId));

            _logger.LogInformation(StartProcessMessage, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value, consumeResult.Key);

            var kafkaMessage = new KafkaMessage(header: new KafkaMessageHeader(operationId, operationParentId),
                                                topicName: consumeResult.Topic,
                                                body: Encoding.UTF8.GetString(consumeResult.Message.Value));

            if (OnMessageReceived != null)
            {
                try
                {
                    await OnMessageReceived(kafkaMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, OnMessageReceivedExceptionMessage, kafkaMessage);
                    return;
                }

                _consumer.StoreOffset(consumeResult);
            }
        }

        private void LogConsumeResult(ConsumeResult<byte[], byte[]> consumeResult)
        {
            const string KafkaMessageInfo = @"KafkaMessage => Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Timestamp(UTC): {Timestamp}, ConsumedAt(UTC): {ConsumedAt}, Latency: {Latency} ms, OperationId: {OperationId}, OperationParentId: {OperationParentId}, EventMessage: {EventMessage}";

            var receivedAt = DateTime.UtcNow;
            var lag = (receivedAt - consumeResult.Timestamp.UtcDateTime).TotalMilliseconds;
            var operationId = Encoding.UTF8.GetString(consumeResult.Headers.GetLastBytes(HeaderProperty.OperationId));
            var operationParentId = Encoding.UTF8.GetString(consumeResult.Headers.GetLastBytes(HeaderProperty.Operation_ParentId));
            var message = Encoding.UTF8.GetString(consumeResult.Message.Value);

            _logger.LogInformation(KafkaMessageInfo
                                    , consumeResult.Topic
                                    , consumeResult.Partition.Value
                                    , consumeResult.Offset.Value
                                    , consumeResult.Timestamp.UtcDateTime
                                    , receivedAt
                                    , lag
                                    , operationId
                                    , operationParentId
                                    , message);
        }
    }
}

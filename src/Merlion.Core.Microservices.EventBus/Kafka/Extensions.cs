using Confluent.Kafka;
using Merlion.Core.Microservices.EventBus.Kafka.AppSettings;
using System;

namespace Merlion.Core.Microservices.EventBus.Kafka
{
    internal static class Extensions
    {
        private const string InvalidBootstrapServersMessage = "BootstrapServers configuration is mandatory.";

        internal static ProducerConfig GetProducerConfig(this KafkaConfiguration kafkaConfig)
        {
            return new ProducerConfig()
            {
                BootstrapServers = kafkaConfig.BootstrapServers ?? throw new Exception(InvalidBootstrapServersMessage),
                EnableIdempotence = kafkaConfig.Producer?.EnableIdempotence ?? true,
            };
        }

        internal static ConsumerConfig GetConsumerConfig(this KafkaConfiguration kafkaConfig)
        {
            const string InvalidConsumerGroupIdMessage = "Consumer.GroupId configuration is mandatory.";

            return new ConsumerConfig
            {
                BootstrapServers = kafkaConfig.BootstrapServers ?? throw new Exception(InvalidBootstrapServersMessage),
                GroupId = kafkaConfig.Consumer?.GroupId ?? throw new Exception(InvalidConsumerGroupIdMessage),
                AutoOffsetReset = kafkaConfig.Consumer?.AutoOffsetResetType,
                EnableAutoCommit = kafkaConfig.Consumer?.EnableAutoCommit,
                EnableAutoOffsetStore = kafkaConfig.Consumer?.EnableAutoOffsetStore
            };
        }
    }
}

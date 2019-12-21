using Confluent.Kafka;
using System;

namespace Merlion.Core.Microservices.EventBus.Kafka.AppSettings
{
    public class KafkaConfiguration
    {
        public string? BootstrapServers { get; set; }
        public KafkaProducer? Producer { get; set; }
        public KafkaConsumer? Consumer { get; set; }

        public class KafkaProducer
        {
            public bool? EnableIdempotence { get; set; }
        }

        public class KafkaConsumer
        {
            public string? GroupId { get; set; }
            public string? AutoOffsetReset { get; set; }
            public AutoOffsetReset? AutoOffsetResetType
            {
                get
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(AutoOffsetReset))
                        {
                            return null;
                        }
                        else
                        {
                            return (AutoOffsetReset)Enum.Parse(typeof(AutoOffsetReset), AutoOffsetReset, true);
                        }
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            /// <summary>
            ///     Automatically store offset of last message provided to application. 
            ///     The offset store is an in-memory store of the next offset to (auto-)commit for each partition.
            ///
            ///     default: false
            /// </summary>
            public bool? EnableAutoOffsetStore { get; set; }
            public bool EnableAutoCommit { get; set; }
            public int? PollIntervalMilliseconds { get; set; }

            /// <summary>
            /// Topic name list listened by the subscriber, use comma as the delimiter to separate different topic name.
            /// </summary>
            /// <example>TopicA,TopicB,TopicC</example>
            public string? Topics { get; set; }
        }
    }
}

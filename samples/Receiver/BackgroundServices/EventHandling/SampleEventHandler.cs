using Merlion.Core.Microservices.EventBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Receiver.Events;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Receiver.BackgroundServices.EventHandling
{
    public class SampleEventHandler : IHostedService
    {
        #region Private Attributes
        private readonly ILogger _logger;
        private readonly IConsumer _subscriber;
        private readonly IServiceProvider _serviceProvider;
        #endregion

        #region Constructor
        public SampleEventHandler(ILogger<SampleEventHandler> logger, IConsumer subscriber, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _subscriber = subscriber;
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region StartAsync
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _subscriber.OnMessageReceived += ReceivedMessageAsync;

            Task.Factory.StartNew(() =>
            {
                _logger.LogInformation($"{nameof(IHostedService.StartAsync)} Start Subscriber.");
                _subscriber.StartConsumerAsync(cancellationToken);
            },
            TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }
        #endregion

        #region StopAsync
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(IHostedService.StopAsync)} Stop Subscriber.");
            _subscriber.StopConsumer();

            return Task.CompletedTask;
        }
        #endregion

        #region ReceivedMessageAsync
        private Task ReceivedMessageAsync(IIntegrationEvent message)
        {
            _logger.LogInformation($"Event ReceivedMessage => Topic: {message.TopicName}");

            _logger.LogInformation($"OperationId:{message.Header.OperationId}, OperationParentId:{message.Header.OperationParentId}");
            var sampleEvent = JsonSerializer.Deserialize<SampleEvent>(message.Body);
            _logger.LogInformation($"SampleEvent.Info", sampleEvent.Info);

            return Task.CompletedTask;
        }
        #endregion
    }
}

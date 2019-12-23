using Merlion.Core.Microservices.EventBus;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Receiver.Events;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Receiver.BackgroundServices.EventHandling
{
    public class SampleEventHandler : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IConsumer _subscriber;
        private readonly IServiceProvider _serviceProvider;
        private readonly TelemetryClient _telemetryClient;

        public SampleEventHandler(
            ILogger<SampleEventHandler> logger, 
            IConsumer subscriber, 
            IServiceProvider serviceProvider,
            TelemetryClient telemetryClient)
        {
            _logger = logger;
            _subscriber = subscriber;
            _serviceProvider = serviceProvider;
            _telemetryClient = telemetryClient;
        }

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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(IHostedService.StopAsync)} Stop Subscriber.");
            _subscriber.StopConsumer();

            return Task.CompletedTask;
        }

        private async Task<Task> ReceivedMessageAsync(IIntegrationEvent message)
        {
            _logger.LogInformation("Start With RequestTelemetry: Kafka Consumer");

            var requestActivity = new Activity("RequestTelemetry: Kafka Consumer");
            var parentId = message.Header.OperationParentId;
            requestActivity.SetParentId(parentId);
            requestActivity.Start();

            var requestOperation = _telemetryClient.StartOperation<RequestTelemetry>(requestActivity);
            
            _logger.LogInformation($"Event ReceivedMessage => Topic: {message.TopicName}");
            var sampleEvent = JsonSerializer.Deserialize<SampleEvent>(message.Body);
            _logger.LogInformation($"Event ReceivedMessage => Message: {sampleEvent}");

            await Task.Delay(1000);

            _telemetryClient.StopOperation(requestOperation);

            return Task.CompletedTask;
        }
    }
}

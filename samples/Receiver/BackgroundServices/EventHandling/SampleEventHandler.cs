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
        #region Private Attributes
        private readonly ILogger _logger;
        private readonly IConsumer _subscriber;
        private readonly IServiceProvider _serviceProvider;
        private readonly TelemetryClient _telemetryClient;
        #endregion

        #region Constructor
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
            var requestTelemetry = new RequestTelemetry { Name = "AppInsights: Kafka Consumer" };
            _logger.LogInformation("Consume Kafka event start");

            Activity requestActivity = new Activity("AppInsights: Kafka Consumer");
            requestActivity.SetParentId(message.Header.OperationParentId);
            requestActivity.Start();

            requestTelemetry.Id = Activity.Current.Id;
            requestTelemetry.Context.Operation.Id = message.Header.OperationId;
            requestTelemetry.Context.Operation.ParentId = message.Header.OperationParentId;

            requestTelemetry.Start();
            _logger.LogInformation($"Event ReceivedMessage => Topic: {message.TopicName}");

            var sampleEvent = JsonSerializer.Deserialize<SampleEvent>(message.Body);
            _logger.LogInformation($"SampleEvent.Info", sampleEvent.Info);

            Task.Delay(1000);

            requestActivity.Stop();
            requestTelemetry.Stop();
            _telemetryClient.TrackRequest(requestTelemetry);

            return Task.CompletedTask;
        }
        #endregion
    }
}

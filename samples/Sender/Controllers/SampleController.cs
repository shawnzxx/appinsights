using Merlion.Core.Microservices.EventBus;
using Merlion.Core.Microservices.EventBus.Kafka;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sender.Events;
using Sender.Extensions;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sender.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SampleController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IProducer _publisher;
        private readonly TelemetryClient _telemetryClient;

        public SampleController(
            ILogger<SampleController> logger, 
            IProducer publisher,
            TelemetryClient telemetryClient)
        {
            _logger = logger;
            _publisher = publisher;
            _telemetryClient = telemetryClient;
        }

        // GET: api/Sample
        // Tutorail for distribute tracing https://medium.com/@tsuyoshiushio/correlation-with-activity-with-application-insights-1-overview-753a48a645fb
        // Official guide for custom tracing https://docs.microsoft.com/en-sg/azure/azure-monitor/app/custom-operations-tracking
        [HttpGet]
        public async Task<IActionResult> SendSample()
        {
            _logger.LogInformation("Start With RequestTelemetry: Http Call");
            var requestActivity = new Activity("RequestTelemetry: Http Call");
            requestActivity.Start();

            var requestOperation = _telemetryClient.StartOperation<RequestTelemetry>(requestActivity);
            _telemetryClient.StopOperation(requestOperation);

            // Start dependency call
            var dependencyActivity = new Activity("DependencyTelemetry: Kafka Producer");
            dependencyActivity.SetParentId(requestActivity.Id);
            dependencyActivity.Start();

            var dependencyOperation = _telemetryClient.StartOperation<DependencyTelemetry>(dependencyActivity);
            
            var sampleEvent = new SampleEvent("I am sample payload");
            var messageHeader = new KafkaMessageHeader(dependencyActivity.RootId, dependencyActivity.Id);
            _logger.LogInformation("Start With DependencyTelemetry: Kafka Producer");
            await _publisher.ProduceAsync(new KafkaMessage(messageHeader, "myTopic", JsonSerializer.Serialize(sampleEvent)));

            _telemetryClient.StopOperation(dependencyOperation);

            return Ok();
        }
    }
}

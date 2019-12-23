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
        // https://medium.com/@tsuyoshiushio/correlation-with-activity-with-application-insights-1-overview-753a48a645fb
        [HttpGet]
        public async Task<IActionResult> SendSample()
        {
            var requestTelemetry = new RequestTelemetry { Name = "AppInsights: HttpTrigger Request" };
            var dependencyTelemetry = new DependencyTelemetry { Name = "AppInsights: Produce Kafka Message" };

            _logger.LogInformation("Start With HttpTrigger Request");

            // Set telemetry ids using Activity
            requestTelemetry.SetActivity(Activity.Current);

            requestTelemetry.Start();
            requestTelemetry.Stop();
            _telemetryClient.TrackRequest(requestTelemetry);

            // Start dependency call
            var dependencyActivity = new Activity("AppInsights: Kafka Produce");
            dependencyActivity.SetParentId(Activity.Current.Id);
            dependencyActivity.Start();

            dependencyTelemetry.SetActivity(dependencyActivity);
            dependencyTelemetry.Start();

            var sampleEvent = new SampleEvent("I am new sample");
            var messageHeader = new KafkaMessageHeader(dependencyActivity.RootId, dependencyActivity.Id);
            _logger.LogInformation("Produce Kafka event start");
            await _publisher.ProduceAsync(new KafkaMessage(messageHeader, "myTopic", JsonSerializer.Serialize(sampleEvent)));
            

            dependencyTelemetry.Stop();
            dependencyActivity.Stop();
            _telemetryClient.TrackDependency(dependencyTelemetry);

            return Ok();
        }
    }
}

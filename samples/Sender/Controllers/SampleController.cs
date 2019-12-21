using Merlion.Core.Microservices.EventBus;
using Merlion.Core.Microservices.EventBus.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sender.Events;
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

        public SampleController(ILogger<SampleController> logger, IProducer publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        // GET: api/Sample
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var sampleEvent = new SampleEvent("123");
            var messageHeader = new KafkaMessageHeader(Activity.Current.Id, string.Empty);

            await _publisher.ProduceAsync(new KafkaMessage(messageHeader, "myTopic", JsonSerializer.Serialize(sampleEvent)));

            _logger.LogDebug("JsonSerializer.Serialize(sampleEvent): " + JsonSerializer.Serialize(sampleEvent));

            _logger.LogInformation("sent");

            return Ok();
        }
    }
}

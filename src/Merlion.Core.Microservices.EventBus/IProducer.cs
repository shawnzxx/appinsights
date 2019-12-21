using System.Threading.Tasks;

namespace Merlion.Core.Microservices.EventBus
{
    public interface IProducer
    {
        Task ProduceAsync(IIntegrationEvent integrationEvent);
    }
}

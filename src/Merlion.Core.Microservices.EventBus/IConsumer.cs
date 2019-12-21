using System;
using System.Threading;
using System.Threading.Tasks;

namespace Merlion.Core.Microservices.EventBus
{
    public interface IConsumer
    {
        event Func<IIntegrationEvent, Task> OnMessageReceived;

        Task StartConsumerAsync(CancellationToken cancellationToken);

        void StopConsumer();
    }
}

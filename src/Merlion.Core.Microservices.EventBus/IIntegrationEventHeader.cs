namespace Merlion.Core.Microservices.EventBus
{
    public interface IIntegrationEventHeader
    {
        public string OperationId { get; }

        public string OperationParentId { get; }
    }
}

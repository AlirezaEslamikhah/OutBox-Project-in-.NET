namespace OutBox_Project.Services.Interfaces
{
    public interface IOutboxMessageService
    {
        Task SendMessage(Guid TransactionId, Guid UserId, Guid AggregateId, string AggregateType, string Type);
    }
}

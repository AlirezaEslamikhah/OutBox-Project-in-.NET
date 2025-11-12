using OutBox_Project.Models;
using OutBox_Project.Services.Interfaces;

namespace OutBox_Project.Services
{
    public class OutboxMessageService : IOutboxMessageService
    {
        private readonly AppDbContext _context;

        public OutboxMessageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(Guid TransactionId, Guid UserId, Guid AggregateId, string AggregateType, string Type)
        {
            long oldSequence = _context.OutboxMessages
                .Where(x => x.AggregateId == AggregateId)
                .OrderByDescending(x => x.Sequence)
                .Select(x => (long?)x.Sequence)
                .FirstOrDefault() ?? 0;

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TransactionId = TransactionId,
                UserId = UserId,
                AggregateId = AggregateId,
                AggregateType = AggregateType,
                Sequence = oldSequence + 1, 
                Type = Type,
                Status = StatusCode.New,
            };

            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync();
        }
    }
}

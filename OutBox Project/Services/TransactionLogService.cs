using Microsoft.EntityFrameworkCore;
using OutBox_Project;
using OutBox_Project.Models;
using OutBox_Project.Services;
using OutBox_Project.Services.Interfaces;

public class TransactionLogService : ITransactionLogService
{
    private readonly AppDbContext _context;
    private readonly IOutboxMessageService _outboxMessageService;

    public TransactionLogService(AppDbContext context, IOutboxMessageService outboxMessageService)
    {
        _context = context;
        _outboxMessageService = outboxMessageService;
    }

    public async Task AddLogAsync(Wallet wallet, Transaction transaction, string description, bool isCredit , Guid userId)
    {
        var log = new TransactionLog
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            PreviousBalance = transaction.IsCredit ? wallet.Balance - transaction.Amount : wallet.Balance + transaction.Amount,
            NewBalance = wallet.Balance,
            Description = description,
            LoggedAt = DateTime.UtcNow
        };
        var outboxType = isCredit ? "credit" : "debit"; 
        await _context.TransactionLogs.AddAsync(log);
        await _outboxMessageService.SendMessage(transaction.Id , userId , wallet.Id , "wallet", outboxType);
        await _context.SaveChangesAsync();
    }


}

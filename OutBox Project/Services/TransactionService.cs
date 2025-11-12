using Microsoft.EntityFrameworkCore;
using OutBox_Project.Models;
using OutBox_Project.Services.Interfaces;

namespace OutBox_Project.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;
        private readonly ITransactionLogService _logService;

        public TransactionService(AppDbContext context, ITransactionLogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<Transaction> CreateTransactionAsync(Wallet wallet, decimal amount, bool isCredit, string description, Guid userId)
        {
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = amount,
                IsCredit = isCredit,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _logService.AddLogAsync(wallet, transaction, description,isCredit , userId);

            return transaction;
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid walletId)
        {
            return await _context.Transactions
                .Where(t => t.WalletId == walletId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}

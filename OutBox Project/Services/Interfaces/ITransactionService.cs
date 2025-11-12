using OutBox_Project.Models;

namespace OutBox_Project.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<Transaction> CreateTransactionAsync(Wallet wallet, decimal amount, bool isCredit, string description , Guid userId );
        Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid walletId);
    }
}

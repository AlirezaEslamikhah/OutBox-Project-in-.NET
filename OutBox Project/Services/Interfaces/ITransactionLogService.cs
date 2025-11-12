using OutBox_Project.Models;

namespace OutBox_Project.Services.Interfaces
{
    public interface ITransactionLogService
    {
        Task AddLogAsync(Wallet wallet, Transaction transaction, string description, bool isCredit , Guid userId);
    }

}

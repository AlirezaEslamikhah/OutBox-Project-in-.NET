namespace OutBox_Project.Services.Interfaces
{
    public interface IWalletService
    {
        Task<decimal> GetBalanceAsync(Guid userId);
        Task AddCreditAsync(Guid UserId, decimal amount, string description);
        Task AddDebitAsync(Guid UserId, decimal amount, string description);
    }

}

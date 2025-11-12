using Microsoft.EntityFrameworkCore;
using OutBox_Project;
using OutBox_Project.Models;
using OutBox_Project.Services.Interfaces;

public class WalletService : IWalletService
{
    private readonly AppDbContext _context;
    private readonly ITransactionService _transactionService;

    public WalletService(AppDbContext context, ITransactionService transactionService)
    {
        _context = context;
        _transactionService = transactionService;
    }

    // 🧾 Get wallet balance by userId
    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Wallet == null)
            throw new InvalidOperationException("User or wallet not found.");

        return user.Wallet.Balance;
    }

    // 💰 Add credit to user’s wallet
    public async Task AddCreditAsync(Guid userId, decimal amount, string description)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found.");

        var wallet = user.Wallet ?? throw new InvalidOperationException("Wallet not found.");

        wallet.Balance += amount;

        await _transactionService.CreateTransactionAsync(wallet, amount, true, description, userId);
        await _context.SaveChangesAsync();
    }

    // 💸 Subtract (debit) from user’s wallet
    public async Task AddDebitAsync(Guid userId, decimal amount, string description)
    {
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found.");

        var wallet = user.Wallet ?? throw new InvalidOperationException("Wallet not found.");

        if (wallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance.");

        wallet.Balance -= amount;

        await _transactionService.CreateTransactionAsync(wallet, amount, false, description,userId);
        await _context.SaveChangesAsync();
    }
}

using Microsoft.AspNetCore.Mvc;
using OutBox_Project.Models;
using OutBox_Project.Services.Interfaces;

namespace OutBox_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        // 🧑 Register new user
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] UserRegisterDto request)
        {
            var user = await _userService.SignUpAsync(request.Username);
            return Ok(new { message = "User registered successfully", user });
        }

        // 🔐 Login existing user
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var user = await _userService.LoginAsync(request.Username);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            return Ok(new { message = "Login successful", user });
        }
    }








    // 💰 Wallet-related operations
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        // 📊 Get user balance
        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetBalance(Guid userId)
        {
            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(new { userId, balance });
        }

        // 💵 Credit wallet
        [HttpPost("credit")]
        public async Task<IActionResult> Credit([FromBody] WalletOperationDto request)
        {
            await _walletService.AddCreditAsync(request.UserId, request.Amount, request.Description);
            return Ok(new { message = "Credit added successfully" });
        }

        // 💸 Debit wallet
        [HttpPost("debit")]
        public async Task<IActionResult> Debit([FromBody] WalletOperationDto request)
        {
            await _walletService.AddDebitAsync(request.UserId, request.Amount, request.Description);
            return Ok(new { message = "Debit processed successfully" });
        }
    }

    // 🔁 Transactions and Logs
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        // 📜 Get all transactions for a user
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetTransactions(Guid walletId)
        {
            var transactions = await _transactionService.GetTransactionsAsync(walletId);
            return Ok(transactions);
        }

    }

    // ---------------------------------
    // 📦 DTOs (Data Transfer Objects)
    // ---------------------------------

    public class UserRegisterDto
    {
        public string Username { get; set; } = string.Empty;
    }

    public class UserLoginDto
    {
        public string Username { get; set; } = string.Empty;
    }

    public class WalletOperationDto
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}

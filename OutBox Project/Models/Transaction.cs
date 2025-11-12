using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OutBox_Project.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        public Guid WalletId { get; set; }
        public Guid TransactionLogId { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsCredit { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Wallet Wallet { get; set; } = null!;
        public TransactionLog Log { get; set; } = null!;
    }
}

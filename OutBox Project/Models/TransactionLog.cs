using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OutBox_Project.Models
{
    public class TransactionLog
    {
        [Key]
        public Guid Id { get; set; }

        //public Guid WalletId { get; set; }
        public Guid TransactionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewBalance { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        //public Wallet Wallet { get; set; } = null!;
        public Transaction Transaction { get; set; } = null!;
    }
}

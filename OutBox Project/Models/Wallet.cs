using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace OutBox_Project.Models
{

    public class Wallet
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCredit { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDebit { get; set; } = 0;

        // Foreign Key
        public Guid UserId { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        //public ICollection<TransactionLog> Logs { get; set; } = new List<TransactionLog>();
    }
}

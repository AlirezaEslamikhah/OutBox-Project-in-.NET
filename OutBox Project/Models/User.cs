using System.ComponentModel.DataAnnotations;

namespace OutBox_Project.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        public Guid WalletId { get; set; }

        public Wallet Wallet { get; set; } = null!;
    }

}

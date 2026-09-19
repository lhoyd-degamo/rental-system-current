using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Penalty
    {
        [Key]
        public int PenaltyID { get; set; }

        [Required(ErrorMessage = "Borrow transaction is required.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid borrow transaction.")]
        public int BorrowID { get; set; }

        [ForeignKey(nameof(BorrowID))]
        public Borrow? Borrow { get; set; }

        [Required(ErrorMessage = "Days late is required.")]
        [Range(0, 365,
            ErrorMessage = "Days late must be between 0 and 365.")]
        public int DaysLate { get; set; }

        [Required(ErrorMessage = "Penalty per day is required.")]
        [Range(0, 1000000,
            ErrorMessage = "Penalty per day cannot be negative.")]
        public decimal PenaltyPerDay { get; set; } = 50m;

        [Required(ErrorMessage = "Penalty amount is required.")]
        [Range(0, 1000000,
            ErrorMessage = "Penalty amount cannot be negative.")]
        public decimal PenaltyAmount { get; set; }
    }
}
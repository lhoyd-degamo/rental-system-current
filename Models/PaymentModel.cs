using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        // Borrow Reference

        [Required(ErrorMessage = "Borrow transaction is required.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid borrow transaction.")]
        public int BorrowID { get; set; }

        [ForeignKey(nameof(BorrowID))]
        public Borrow? Borrow { get; set; }

        // Payment Method

        [Required(ErrorMessage = "Payment method is required.")]
        [StringLength(30,
            ErrorMessage = "Payment method cannot exceed 30 characters.")]
        public string PaymentMethod { get; set; } = "Cash";

        // Payment Amount

        [Required(ErrorMessage = "Payment amount is required.")]
        [Range(0.01, 1000000,
            ErrorMessage = "Payment amount must be greater than 0.")]
        public decimal AmountPaid { get; set; }

        // Payment Status

        [Required(ErrorMessage = "Payment status is required.")]
        [StringLength(30,
            ErrorMessage = "Payment status cannot exceed 30 characters.")]
        public string PaymentStatus { get; set; } = "Paid";

        // Payment Date

        [Required(ErrorMessage = "Payment date is required.")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
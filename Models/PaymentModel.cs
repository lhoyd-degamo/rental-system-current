using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }


        // Borrow Reference

        public int BorrowID { get; set; }

        [ForeignKey(nameof(BorrowID))]
        public Borrow? Borrow { get; set; }


        // Payment Method

        [Required]
        public string PaymentMethod { get; set; } = "Cash";


        // Automatically gets Item Amount

        public decimal AmountPaid { get; set; }


        // Payment Status

        [Required]
        public string PaymentStatus { get; set; } = "Paid";


        // Payment Date

        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
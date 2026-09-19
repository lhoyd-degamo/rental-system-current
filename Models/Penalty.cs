using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Penalty
    {
        [Key]
        public int PenaltyID { get; set; }

        public int BorrowID { get; set; }

        [ForeignKey(nameof(BorrowID))]
        public Borrow? Borrow { get; set; }

        public int DaysLate { get; set; }

        public decimal PenaltyPerDay { get; set; } = 50m;

        public decimal PenaltyAmount { get; set; }
    }
}
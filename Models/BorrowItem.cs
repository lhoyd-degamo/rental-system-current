using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class BorrowItem
    {
        //kani para makabutang og multiple item sa borrow
        [Key]
        public int BorrowItemID { get; set; }

        public int BorrowID { get; set; }

        [ForeignKey(nameof(BorrowID))]
        public Borrow? Borrow { get; set; }

        public int ItemID { get; set; }

        [ForeignKey(nameof(ItemID))]
        public Item? Item { get; set; }
    }
}
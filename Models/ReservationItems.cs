using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class ReservationItems
    {
        [Key]
        public int ReservationItemID { get; set; }

        public int ReservationID { get; set; }

        [ForeignKey(nameof(ReservationID))]
        public Reservation? Reservation { get; set; }

        public int ItemID { get; set; }

        [ForeignKey(nameof(ItemID))]
        public Item? Item { get; set; }
    }
}
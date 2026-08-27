using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }

        // Human friendly code shown to users instead of the raw numeric ID
        // e.g. "TUX001", "GWN014". Generated automatically when an item is created.
        public string ItemCode { get; set; } = "";

        [Required (ErrorMessage ="Item name is required")]
        public string ItemName { get; set; }

        [Required(ErrorMessage ="Description is required")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage ="Amount is required")]
        [Range(0.1, 1000000, ErrorMessage="The ammount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Size is required")]
        public string Size { get; set; } = "";

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000, ErrorMessage = "Quantity must be at least 1")]


        public int Quantity { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Available";

        public int CatID { get; set; }

        [ForeignKey(nameof(CatID))]
        public Category? Category { get; set; }
    }
}
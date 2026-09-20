using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Item
    {
        [Key]
        public int ItemID { get; set; }


        [StringLength(20,
            ErrorMessage = "Item code cannot exceed 20 characters.")]
        public string ItemCode { get; set; } = "";


        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Item name must be between 2 and 100 characters.")]
        public string ItemName { get; set; } = "";


        [Required(ErrorMessage = "Description is required.")]
        [StringLength(500, MinimumLength = 2,
            ErrorMessage = "Description must be between 2 and 500 characters.")]
        public string Description { get; set; } = "";


        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000,
            ErrorMessage = "Rental price must be greater than 0.")]
        public decimal Amount { get; set; }


        [Required(ErrorMessage = "Size is required.")]
        [StringLength(20,
            ErrorMessage = "Size cannot exceed 20 characters.")]
        public string Size { get; set; } = "";


        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 1000,
            ErrorMessage = "Quantity must be between 1 and 1000.")]
        public int Quantity { get; set; }


        [Required(ErrorMessage = "Status is required.")]
        [StringLength(30,
            ErrorMessage = "Status cannot exceed 30 characters.")]
        public string Status { get; set; } = "Available";


        [Required(ErrorMessage = "Category is required.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a valid category.")]
        public int CatID { get; set; }


        [ForeignKey(nameof(CatID))]
        public Category? Category { get; set; }


        // =========================================
        // IMAGE
        // =========================================

        [StringLength(255,
            ErrorMessage = "Image path cannot exceed 255 characters.")]
        public string ImagePath { get; set; } = "";
    }
}
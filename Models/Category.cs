using System.ComponentModel.DataAnnotations;

namespace CRUD.Models
{
    public class Category
    {
        [Key]
        public int CatID { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Category name must be between 2 and 100 characters.")]
        public string CatName { get; set; } = "";
    }
}
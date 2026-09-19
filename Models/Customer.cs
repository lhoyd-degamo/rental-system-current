using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CRUD.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Customer name must be between 2 and 100 characters.")]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150,
            ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(09\d{9}|\+639\d{9})$",
            ErrorMessage = "Please enter a valid Philippine mobile number.")]
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(250, MinimumLength = 5,
            ErrorMessage = "Address must be between 5 and 250 characters.")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "Please select an ID type.")]
        [StringLength(50,
            ErrorMessage = "ID type cannot exceed 50 characters.")]
        public string IDType { get; set; } = "";

        [Required(ErrorMessage = "ID number is required.")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "ID number must be between 3 and 50 characters.")]
        public string IDNumber { get; set; } = "";

        public ICollection<Borrow> Borrows { get; set; }
            = new List<Borrow>();
    }
}
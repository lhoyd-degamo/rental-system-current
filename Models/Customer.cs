using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CRUD.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required")]
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = "";

        // Customer identification
        [Required(ErrorMessage = "ID type is required")]
        public string IDType { get; set; } = "";

        [Required(ErrorMessage = "ID number is required")]
        public string IDNumber { get; set; } = "";

        public ICollection<Borrow> Borrows { get; set; }
            = new List<Borrow>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Borrow
    {
        [Key]
        public int BorrowID { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid customer.")]
        public int CustomerID { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public Customer? Customer { get; set; }

        [Required(ErrorMessage = "Item is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid item.")]
        public int ItemID { get; set; }

        [ForeignKey(nameof(ItemID))]
        public Item? Item { get; set; }

        public ICollection<BorrowItem> BorrowItems { get; set; }
            = new List<BorrowItem>();

        [Required(ErrorMessage = "Borrow date is required.")]
        public DateTime BorrowDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Return date is required.")]
        public DateTime ReturnDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
        public int Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(30, ErrorMessage = "Status cannot exceed 30 characters.")]
        public string Status { get; set; } = "Booked";

        [NotMapped]
        public bool IsOverdue =>
            Status != "Returned" &&
            ReturnDate.Date < DateTime.Today;

        [NotMapped]
        [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters.")]
        public string NewCustomerName { get; set; } = "";

        [NotMapped]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string NewCustomerEmail { get; set; } = "";

        [NotMapped]
        [RegularExpression(@"^(09\d{9}|\+639\d{9})$",
            ErrorMessage = "Please enter a valid Philippine mobile number.")]
        public string NewCustomerPhone { get; set; } = "";

        [NotMapped]
        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string NewCustomerAddress { get; set; } = "";

        [NotMapped]
        [StringLength(50, ErrorMessage = "ID type cannot exceed 50 characters.")]
        public string NewCustomerIDType { get; set; } = "";

        [NotMapped]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "ID number must be between 3 and 50 characters.")]
        public string NewCustomerIDNumber { get; set; } = "";

        [NotMapped]
        public string PhoneNumber
        {
            get => Customer?.PhoneNumber ?? NewCustomerPhone;
            set => NewCustomerPhone = value;
        }

        [NotMapped]
        public string Address
        {
            get => Customer?.Address ?? NewCustomerAddress;
            set => NewCustomerAddress = value;
        }

        [NotMapped]
        public List<int> SelectedItemIDs { get; set; }
            = new List<int>();
    }
}
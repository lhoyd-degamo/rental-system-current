using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Borrow
    {
        // kani sa borrow ni

        [Key]
        public int BorrowID { get; set; }

        // Customer Navigation
        public int CustomerID { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public Customer? Customer { get; set; }

        // Item Navigation
        public int ItemID { get; set; }

        [ForeignKey(nameof(ItemID))]
        public Item? Item { get; set; }

        // Multiple Items
        public ICollection<BorrowItem> BorrowItems { get; set; }
            = new List<BorrowItem>();

        // Customer ID Information
        [Required(ErrorMessage = "ID type is required")]
        public string IDType { get; set; } = "";

        [Required(ErrorMessage = "ID number is required")]
        public string IDNumber { get; set; } = "";

        // Rental Information
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        public DateTime ReturnDate { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public string Status { get; set; } = "Borrowed";

        // Temporary Customer Fields (Not saved in Borrows table)
        [NotMapped]
        public string NewCustomerName { get; set; } = "";

        [NotMapped]
        public string NewCustomerEmail { get; set; } = "";

        [NotMapped]
        public string NewCustomerPhone { get; set; } = "";

        [NotMapped]
        public string NewCustomerAddress { get; set; } = "";

        // Compatibility Properties
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

        // Selected Items from Create Borrow
        [NotMapped]
        public List<int> SelectedItemIDs { get; set; }
            = new List<int>();
    }
}
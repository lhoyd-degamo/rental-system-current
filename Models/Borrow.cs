using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Borrow
    {
        [Key]
        public int BorrowID { get; set; }

        public int CustomerID { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public Customer? Customer { get; set; }

        public int ItemID { get; set; }

        [ForeignKey(nameof(ItemID))]
        public Item? Item { get; set; }

        public ICollection<BorrowItem> BorrowItems { get; set; }
            = new List<BorrowItem>();

        public DateTime BorrowDate { get; set; } = DateTime.Today;

        public DateTime ReturnDate { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public string Status { get; set; } = "Booked";

        [NotMapped]
        public bool IsOverdue =>
            Status != "Returned" &&
            ReturnDate.Date < DateTime.Today;

        [NotMapped]
        public string NewCustomerName { get; set; } = "";

        [NotMapped]
        public string NewCustomerEmail { get; set; } = "";

        [NotMapped]
        public string NewCustomerPhone { get; set; } = "";

        [NotMapped]
        public string NewCustomerAddress { get; set; } = "";

        [NotMapped]
        public string NewCustomerIDType { get; set; } = "";

        [NotMapped]
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
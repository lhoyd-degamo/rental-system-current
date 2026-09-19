using crud.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRUD.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        // CUSTOMER
        [Required]
        public int CustomerID { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public Customer? Customer { get; set; }

        // For Admin creating a new customer
        [NotMapped]
        [Display(Name = "Customer Name")]
        public string? NewCustomerName { get; set; }

        [NotMapped]
        [EmailAddress]
        [Display(Name = "Customer Email")]
        public string? NewCustomerEmail { get; set; }

        [NotMapped]
        [Display(Name = "Customer Phone")]
        public string? NewCustomerPhone { get; set; }

        [NotMapped]
        [Display(Name = "Customer Address")]
        public string? NewCustomerAddress { get; set; }


        // VALID ID
        [Required(ErrorMessage = "ID Type is required.")]
        [Display(Name = "ID Type")]
        public string? IDType { get; set; }

        [Required(ErrorMessage = "ID Number is required.")]
        [Display(Name = "ID Number")]
        public string? IDNumber { get; set; }


        // MULTIPLE ITEMS
        public ICollection<ReservationItems> ReservationItems { get; set; }
            = new List<ReservationItems>();

        // Used by the Create/Edit form
        [NotMapped]
        public List<int> SelectedItemIDs { get; set; }
            = new List<int>();


        // RESERVATION INFORMATION
        [Required]
        public DateTime ReservationDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Reservation Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Return date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Reservation End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);


        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;


        // Pending / Confirmed / Cancelled / Completed
        [Required]
        public string Status { get; set; } = "Pending";


        public string? Notes { get; set; }
    }
}
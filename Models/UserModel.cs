using System.ComponentModel.DataAnnotations;
namespace crud.Models
{
    public class UserModel
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        public string UserName { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserRole { get; set; } = "Admin";
    }
}
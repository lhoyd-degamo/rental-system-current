using System.ComponentModel.DataAnnotations;

namespace CRUD.Models
{
    public class Category
    {
        //github
        [Key]
        public int CatID { get; set; }

        public string CatName { get; set; }
    }
}
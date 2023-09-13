using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Models
{
    [Table("Customers")]
    public class CustomCustomer
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [Required]
        public CustomAddr Address { get; set; }
    }
}

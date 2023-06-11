using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Models
{
    [Table("Customers")]
    public class Customer
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }
}

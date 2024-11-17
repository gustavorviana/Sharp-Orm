using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    [Table("Customers")]
    public class CustomCustomer
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [ForeignKey("address_id")]
        public CustomAddr Address { get; set; }
    }
}

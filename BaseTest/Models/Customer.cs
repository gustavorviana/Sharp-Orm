using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    [Table("Customers")]
    public class Customer
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        [Column("address_id")]
        public int? AddressId { get; set; }

        [ForeignKey("address_id")]
        public Address Address { get; set; }
    }
}

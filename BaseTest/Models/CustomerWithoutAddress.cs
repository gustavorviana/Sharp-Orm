using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    public class CustomerWithoutAddress
    {
        public uint Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }

        [Column("address_id")]
        public int? AddressId { get; set; }
    }
}

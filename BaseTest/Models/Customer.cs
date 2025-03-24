using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    [Table("Customers")]
    public class Customer : CustomerWithoutAddress
    {
        [ForeignKey("address_id")]
        public Address? Address { get; set; }
    }
}

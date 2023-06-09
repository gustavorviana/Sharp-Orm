using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Models
{
    [Table("Orders")]
    public class Order
    {
        public int Id { get; set; }
        [Column("customer_id")]
        public int CustomerId { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }

        [ForeignKey("customer_id")]
        public Customer Customer { get; set; }
    }
}

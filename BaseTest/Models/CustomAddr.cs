using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    public class CustomAddr
    {
        public int Id { get; set; }

        [Column("address_id")]
        public int CustomId { get; set; }
    }
}

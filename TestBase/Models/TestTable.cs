using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    public class ExtendedTestTable : TestTable
    {
        public string ExtendedProp { get; set; }
    }

    public class TestTable
    {
        [Key, Column(Order = 0)]
        public int Id { get; set; }
        [Key, Column(Order = 1)]
        public int Id2 { get; set; }
        public string Name { get; set; }
        public string Nick { get; set; }
        [Column("record_created")]
        public DateTime? CreatedAt { get; set; }
        public decimal Number { get; set; }
        [Column("custom_id")]
        public Guid? CustomId { get; set; }
        [Column("custom_status")]
        public Status CustomStatus { get; set; }
    }
}

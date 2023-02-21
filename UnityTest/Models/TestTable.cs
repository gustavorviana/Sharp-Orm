using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Models
{
    public class ExtendedTestTable : TestTable
    {
        public string ExtendedProp { get; set; }
    }

    public class TestTable
    {
        [Key]
        public int Id { get; set; }
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

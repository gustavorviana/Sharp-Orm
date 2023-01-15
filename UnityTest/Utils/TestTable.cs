using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnityTest.Utils
{
    public class TestTable
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Nick { get; set; }
        [Column("record_created")]
        public DateTime? CreatedAt { get; set; }
    }
}

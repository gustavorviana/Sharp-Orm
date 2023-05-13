using System.ComponentModel.DataAnnotations;

namespace UnityTest.Models
{
    internal class ModelDoublePrimary
    {
        [Key]
        public int Id { get; set; }
        [Key]
        public int Id2 { get; set; }
        public string Name { get; set; }
    }
}

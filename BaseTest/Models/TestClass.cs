using System.ComponentModel.DataAnnotations;

namespace BaseTest.Models
{
    public class TestClass
    {
        [Key]
        public int MyId { get; set; }
        public string? MyName { get; set; }
        public DateTime MyDate { get; set; }
        public TimeSpan MyTime { get; set; }
        public byte MyByte { get; set; }
        public Status MyEnum { get; set; }
        public Guid? MyGuid { get; set; }
    }
}

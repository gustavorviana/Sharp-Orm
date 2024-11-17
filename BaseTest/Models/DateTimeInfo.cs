using System.ComponentModel.DataAnnotations;

namespace BaseTest.Models
{
    public class DateTimeInfo
    {
        [Key]
        public int MyId { get; set; }

        public DateTime DateTime { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public DateOnly DateOnly { get; set; }

        public TimeOnly TimeOnly { get; set; }
    }
}

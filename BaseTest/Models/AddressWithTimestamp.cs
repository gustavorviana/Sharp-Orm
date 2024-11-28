using SharpOrm;

namespace BaseTest.Models
{
    [HasTimestamp]
    public record AddressWithTimeStamp(int Id) : Address(Id)
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

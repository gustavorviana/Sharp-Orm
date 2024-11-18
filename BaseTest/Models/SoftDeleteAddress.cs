using SharpOrm;

namespace BaseTest.Models
{
    [SoftDelete]
    public record SoftDeleteAddress(int Id) : Address(Id)
    {
        public bool Deleted { get; set; }
    }
}

using SharpOrm;

namespace BaseTest.Models
{
    [SoftDelete(DateColumnName = "deleted_at")]
    public record SoftDeleteDateAddress(int Id) : Address(Id)
    {
        public bool Deleted { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}

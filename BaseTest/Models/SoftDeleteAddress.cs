using SharpOrm;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models
{
    [SoftDelete(DateColumnName = "deleted_at")]
    public record SoftDeleteAddress(int Id) : Address(Id)
    {
        public bool Deleted { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}

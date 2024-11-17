using BaseTest.Utils;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaseTest.Models.Blob
{
    [Table(Consts.Files.TABLE)]
    public class BytesModel
    {
        public int Id { get; set; }
        [Column(Consts.Files.BINARY)]
        public byte[]? File { get; set; }
    }
}

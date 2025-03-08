using BaseTest.Utils;
using SharpOrm.DataTranslation;
using System.ComponentModel.DataAnnotations;

namespace QueryTest.DataTranslation
{
    public class ObjectReaderTests : DbMockFallbackTest
    {
        private readonly ObjectReader _reader;

        public ObjectReaderTests()
        {
            _reader = new ObjectReader(Translation.GetTable(typeof(Item)))
            {
                Validate = true
            };
        }

        [Fact]
        public void ValidateWithOptional()
        {
            _reader.Except<Item>(x => x.Name);
            _ = _reader.ReadCells(new Item()).ToArray();
        }

        [Fact]
        public void Validate()
        {
            var result = Assert.Throws<ValidationException>(() => _reader.ReadCells(new Item()).ToArray());
            Assert.Equal("The Item field is required.", result.Message);
        }

        private class Item
        {
            public int Id { get; set; }

            [Required]
            public string? Name { get; set; }

            public string? Description { get; set; }
        }
    }
}

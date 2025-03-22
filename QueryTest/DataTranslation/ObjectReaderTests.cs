using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm.DataTranslation;
using System.ComponentModel.DataAnnotations;

namespace QueryTest.DataTranslation
{
    public class ObjectReaderTests : DbMockFallbackTest
    {
        [Fact]
        public void ValidateWithOptionalTest()
        {
            var reader = GetObjectReaderWithValidation();

            reader.Except<Item>(x => x.Name);
            _ = reader.ReadCells(new Item()).ToArray();
        }

        [Fact]
        public void ValidateTest()
        {
            var result = Assert.Throws<ValidationException>(() => GetObjectReaderWithValidation().ReadCells(new Item()).ToArray());
            Assert.Equal("The Item field is required.", result.Message);
        }

        [Fact]
        public void ColumnsTest()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.ReadPk = true;
            reader.IsCreate = true;

            string[] expected = ["Id", "Name", "Street", "City", "CreatedAt", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void ColumnsWithoutTimestampsTest()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.IgnoreTimestamps = true;
            reader.ReadPk = true;

            string[] expected = ["Id", "Name", "Street", "City"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void CreateColumnsTest()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.IsCreate = true;

            string[] expected = ["Name", "Street", "City", "CreatedAt", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }


        [Fact]
        public void UpdateColumnsTest()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.ReadPk = true;

            string[] expected = ["Id", "Name", "Street", "City", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        private ObjectReader GetObjectReaderWithValidation()
        {
            return new ObjectReader(Translation.GetTable(typeof(Item)))
            {
                Validate = true
            };
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

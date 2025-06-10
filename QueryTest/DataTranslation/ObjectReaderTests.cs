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
        public void ReadCellsWithValidKeyTest()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "My Name",
                Description = "My Description",
                SubItem = new SubItem
                {
                    Id = 2,
                    Name = "Sub Item Name"
                }
            }).ToArray();

            Assert.Equal(3, cells.Length);
            Assert.Equal("Id", cells[0].Name);
            Assert.Equal("Name", cells[1].Name);
            Assert.Equal("Description", cells[2].Name);

            Assert.Equal(1, cells[0].Value);
            Assert.Equal("My Name", cells[1].Value);
            Assert.Equal("My Description", cells[2].Value);

            cells = [.. reader.ReadCells(new Item())];
            Assert.DoesNotContain(cells, x => x.Name == "Id");
        }

        [Fact]
        public void ReadCellsWithInvalidValidKeyTest()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.All;

            var cells = reader.ReadCells(new Item()).ToArray();
            Assert.Equal(3, cells.Length);
            Assert.Equal("Id", cells[0].Name);
            Assert.Equal("Name", cells[1].Name);
            Assert.Equal("Description", cells[2].Name);

            Assert.Equal(0, cells[0].Value);
            Assert.Equal(DBNull.Value, cells[1].Value);
            Assert.Equal(DBNull.Value, cells[2].Value);

            cells = [.. reader.ReadCells(new Item { Id = 1 })];
            Assert.Equal(3, cells.Length);
        }

        [Fact]
        public void ReadCellsWithoutKeyTest()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.None;

            var cells = reader.ReadCells(new Item { Id = 1 }).ToArray();

            Assert.Equal(2, cells.Length);

            cells = [.. reader.ReadCells(new Item())];
            Assert.Equal(2, cells.Length);
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
            reader.ReadPk = false;

            string[] expected = ["Name", "Street", "City", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        private ObjectReader GetObjectReaderWithValidation(bool validate = true)
        {
            return new ObjectReader(Translation.GetTable(typeof(Item)))
            {
                Validate = validate
            };
        }

        private class Item
        {
            public int Id { get; set; }

            [Required]
            public string? Name { get; set; }

            public string? Description { get; set; }

            public SubItem? SubItem { get; set; }
        }

        private class SubItem
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}

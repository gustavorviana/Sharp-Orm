using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueryTest.DataTranslation
{
    public class ObjectReaderTests : DbMockFallbackTest
    {
        public ObjectReaderTests()
        {
            MakeUnsafe();
        }

        [Fact]
        public void Except_WhenCalledWithOptionalValidation_ShouldExecuteSuccessfully()
        {
            var reader = GetObjectReaderWithValidation();

            reader.Except<Item>(x => x.Name);
            _ = reader.ReadCells(new Item()).ToArray();
        }

        [Fact]
        public void ReadCells_WhenPrimaryKeyModeIsValidOnlyAndObjectHasValidKey_ShouldReturnOnlyValidKeyColumns()
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
        public void ReadCells_Test2()
        {
            ResetConfig();
            var mapper = new ModelMapper<Item>(Translation);
            mapper.MapNested(x => x.SubItem);
            mapper.Build();

            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.ValidOnly;
            reader.Except<Item>(x => new { x.SubItem!.Id, x.SubItem!.Name });

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
        public void ReadCells_WhenPrimaryKeyModeIsAllAndObjectHasInvalidKey_ShouldReturnAllColumnsWithDefaultValues()
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
        public void ReadCells_WhenPrimaryKeyModeIsNone_ShouldExcludePrimaryKeyColumn()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.None;

            var cells = reader.ReadCells(new Item { Id = 1 }).ToArray();

            Assert.Equal(2, cells.Length);

            cells = [.. reader.ReadCells(new Item())];
            Assert.Equal(2, cells.Length);
        }

        [Fact]
        public void ReadCells_WhenValidationEnabledAndRequiredFieldMissing_ShouldThrowValidationException()
        {
            var result = Assert.Throws<ValidationException>(() => GetObjectReaderWithValidation().ReadCells(new Item()).ToArray());
            Assert.Equal("The Name field is required.", result.Message);
        }

        [Fact]
        public void GetColumnNames_WhenIsCreateTrueAndReadPkTrue_ShouldReturnAllExpectedColumns()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.ReadPk = true;
            reader.IsCreate = true;

            string[] expected = ["Id", "Name", "Street", "City", "CreatedAt", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void GetColumnNames_WhenReadDatabaseGeneratedTrue_ShouldIncludeDatabaseGeneratedColumns()
        {
            var reader = ObjectReader.OfType<Item>(Translation);
            reader.ReadPk = true;
            reader.IsCreate = true;
            reader.ReadDatabaseGenerated = true;

            string[] expected = ["Id", "Name", "Description", "IsValidName"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void GetColumnNames_WhenReadDatabaseGeneratedFalse_ShouldExcludeDatabaseGeneratedColumns()
        {
            var reader = ObjectReader.OfType<Item>(Translation);
            reader.ReadPk = true;
            reader.IsCreate = true;
            reader.ReadDatabaseGenerated = false;

            string[] expected = ["Id", "Name", "Description"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void GetColumnNames_WhenIgnoreTimestampsTrue_ShouldExcludeTimestampColumns()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.IgnoreTimestamps = true;
            reader.ReadPk = true;

            string[] expected = ["Id", "Name", "Street", "City"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void GetColumnNames_WhenIsCreateTrue_ShouldExcludePrimaryKeyFromColumnList()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.IsCreate = true;

            string[] expected = ["Name", "Street", "City", "CreatedAt", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void GetColumnNames_WhenIsCreateFalse_ShouldIncludeUpdateTimestampOnly()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.ReadPk = false;

            string[] expected = ["Name", "Street", "City", "UpdatedAt"];

            var columns = reader.GetColumnNames();
            CollectionAssert.ContainsAll(expected, columns);
        }

        [Fact]
        public void ReadCells_WhenObjectTypeDoesNotMatch_ShouldThrowArgumentException()
        {
            var reader = GetObjectReaderWithValidation(false);
            Assert.Throws<ArgumentException>(() => reader.ReadCells(new Customer()).ToArray());
        }

        [Fact]
        public void ReadCells_WhenOnlySpecificColumnsSelected_ShouldReturnOnlySelectedColumns()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.Only("Name", "Description");

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test Name",
                Description = "Test Description"
            }).ToArray();

            Assert.Equal(3, cells.Length);
            Assert.Contains(cells, c => c.Name == "Name");
            Assert.Contains(cells, c => c.Name == "Description");
            Assert.DoesNotContain(cells, c => c.Name == "Id");
        }

        [Fact]
        public void ReadCells_WhenOnlyWithExpressionUsed_ShouldReturnOnlySpecifiedProperty()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.Only<Item>(x => x.Name);

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test Name",
                Description = "Test Description"
            }).ToArray();

            Assert.Single(cells);
            Assert.Equal("Name", cells[0].Name);
            Assert.Equal("Test Name", cells[0].Value);
        }

        [Fact]
        public void ReadCells_WhenExceptSpecificColumnsUsed_ShouldExcludeSpecifiedColumns()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.Except("Id");

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test Name",
                Description = "Test Description"
            }).ToArray();

            Assert.Equal(3, cells.Length);
            Assert.DoesNotContain(cells, c => c.Name == "Id");
            Assert.Contains(cells, c => c.Name == "Name");
            Assert.Contains(cells, c => c.Name == "Description");
        }

        [Fact]
        public void ReadCells_WhenExceptWithExpressionUsed_ShouldExcludeSpecifiedProperty()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.ReadPk = true;
            reader.Except<Item>(x => x.Description);

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test Name",
                Description = "Test Description"
            }).ToArray();

            Assert.Equal(2, cells.Length);
            Assert.DoesNotContain(cells, c => c.Name == "Description");
            Assert.Contains(cells, c => c.Name == "Id");
            Assert.Contains(cells, c => c.Name == "Name");
        }

        [Fact]
        public void ReadRows_WhenMultipleObjectsProvided_ShouldReturnCorrespondingRows()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var items = new[]
            {
               new Item { Id = 1, Name = "Item 1", Description = "Desc 1" },
               new Item { Id = 2, Name = "Item 2", Description = "Desc 2" }
           };

            var rows = reader.ReadRows(items);

            Assert.Equal(2, rows.Length);
            Assert.Equal(3, rows[0].Cells.Length);
            Assert.Equal(3, rows[1].Cells.Length);

            Assert.Equal(1, rows[0].Cells[0].Value);
            Assert.Equal("Item 1", rows[0].Cells[1].Value);
            Assert.Equal(2, rows[1].Cells[0].Value);
            Assert.Equal("Item 2", rows[1].Cells[1].Value);
        }

        [Fact]
        public void ReadRow_WhenSingleObjectProvided_ShouldReturnSingleRow()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var item = new Item { Id = 5, Name = "Single Item", Description = "Single Desc" };
            var row = reader.ReadRow(item);

            Assert.Equal(3, row.Cells.Length);
            Assert.Equal(5, row.Cells[0].Value);
            Assert.Equal("Single Item", row.Cells[1].Value);
            Assert.Equal("Single Desc", row.Cells[2].Value);
        }

        [Fact]
        public void HasValidKey_WhenObjectHasValidKey_ShouldReturnTrue()
        {
            var reader = GetObjectReaderWithValidation(false);

            var item = new Item { Id = 100, Name = "Valid Key Item" };
            var hasValidKey = reader.HasValidKey(item);

            Assert.True(hasValidKey);
        }

        [Fact]
        public void HasValidKey_WhenObjectHasInvalidKey_ShouldReturnFalse()
        {
            var reader = GetObjectReaderWithValidation(false);

            var item = new Item { Id = 0, Name = "Invalid Key Item" };
            var hasValidKey = reader.HasValidKey(item);

            Assert.False(hasValidKey);
        }

        [Fact]
        public void ReadCells_WhenReadDatabaseGeneratedTrue_ShouldIncludeDatabaseGeneratedColumns()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.ReadDatabaseGenerated = true;
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test",
                IsValidName = true
            }).ToArray();

            var dbGeneratedCell = cells.FirstOrDefault(c => c.Name == "IsValidName");
            Assert.NotNull(dbGeneratedCell);
            Assert.Equal(1, dbGeneratedCell.Value);
        }

        [Fact]
        public void ReadCells_WhenReadDatabaseGeneratedFalse_ShouldExcludeDatabaseGeneratedColumns()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.ReadDatabaseGenerated = false;
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test",
                IsValidName = true
            }).ToArray();

            var dbGeneratedCell = cells.FirstOrDefault(c => c.Name == "IsValidName");
            Assert.Null(dbGeneratedCell);
        }

        [Fact]
        public void ReadCells_WhenValidationEnabledAndSpecificPropertyInvalid_ShouldThrowValidationExceptionWithPropertyName()
        {
            var reader = GetObjectReaderWithValidation(true);

            var item = new Item
            {
                Id = 1,
                Name = null,
                Description = new string('x', 1000)
            };

            var exception = Assert.Throws<ValidationException>(() => reader.ReadCells(item).ToArray());
            Assert.Contains("Name", exception.Message);
        }

        [Fact]
        public void ReadCells_WhenForeignKeyObjectIsNull_ShouldHandleNullForeignKey()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.ReadFk = true;
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var item = new Item
            {
                Id = 1,
                Name = "Item without SubItem",
                SubItem = null
            };

            var cells = reader.ReadCells(item).ToArray();

            var subItemIdCell = cells.FirstOrDefault(c => c.Name == "SubItemId");
            Assert.Null(subItemIdCell?.Value);
        }

        [Fact]
        public void ReadCells_WhenCombiningOnlyAndExceptFilters_ShouldApplyBothFilters()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.Only("Name", "Description", "Id");
            reader.Except("Description");
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test Name",
                Description = "Should be excluded"
            }).ToArray();

            Assert.Equal(2, cells.Length);
            Assert.Contains(cells, c => c.Name == "Id");
            Assert.Contains(cells, c => c.Name == "Name");
            Assert.DoesNotContain(cells, c => c.Name == "Description");
        }

        [Fact]
        public void ReadCells_WhenIgnoreTimestampsTrue_ShouldExcludeTimestampColumns()
        {
            var reader = ObjectReader.OfType<AddressWithTimeStamp>(Translation);
            reader.IgnoreTimestamps = true;
            reader.IsCreate = true;

            var address = new AddressWithTimeStamp(1)
            {
                Name = "Test Address",
                Street = "Test Street",
                City = "Test City"
            };

            var cells = reader.ReadCells(address).ToArray();

            Assert.DoesNotContain(cells, c => c.Name == "CreatedAt");
            Assert.DoesNotContain(cells, c => c.Name == "UpdatedAt");
        }

        [Fact]
        public void ReadCells_WhenExceptMultipleColumns_ShouldExcludeAllSpecifiedColumns()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.Except("Id", "Description");
            reader.PrimaryKeyMode = ReadMode.ValidOnly;

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "Test Name",
                Description = "Test Description"
            }).ToArray();

            Assert.Equal(1, cells.Length);
            Assert.Contains(cells, c => c.Name == "Name");
            Assert.DoesNotContain(cells, c => c.Name == "Id");
            Assert.DoesNotContain(cells, c => c.Name == "Description");
        }

        [Fact]
        public void Create_WhenUsingObsoleteMethod_ShouldCreateObjectReaderInstance()
        {
#pragma warning disable CS0618
            var reader = ObjectReader.Create<Item>(Translation);
#pragma warning restore CS0618

            Assert.NotNull(reader);
            Assert.IsType<ObjectReader>(reader);
        }

        [Fact]
        public void ReadCells_WhenOnlyWithLowercaseName_ShouldMatchPropertyCaseInsensitive()
        {
            var reader = GetObjectReaderWithValidation(false);
            reader.PrimaryKeyMode = ReadMode.ValidOnly;
            reader.Only("name");

            var cells = reader.ReadCells(new Item
            {
                Id = 1,
                Name = "test name",
                Description = "Test Description"
            }).ToArray();

            Assert.Single(cells);
            Assert.Equal("Name", cells[0].Name);
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

            [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
            public bool IsValidName { get; set; }
        }

        private class SubItem
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }

        [Fact]
        public void ReadCells_WithCircularObjectReference_ShouldNotCauseStackOverflow()
        {
            // Arrange
            var registry = Translation;
            var table = registry.GetTable(typeof(CircularObjectA));
            var reader = new ObjectReader(table);

            var objA = new CircularObjectA { Id = 1, Name = "A" };
            var objB = new CircularObjectB { Id = 2, Name = "B" };
            objA.ObjectB = objB;
            objB.ObjectA = objA;

            // Act
            var cells = reader.ReadCells(objA).ToList();

            // Assert
            Assert.NotNull(cells);

            Assert.True(cells.Count > 0);
        }

        [Fact]
        public void ReadCells_WithNullNestedObject_ShouldHandleGracefully()
        {
            // Arrange
            var registry = Translation;
            var table = registry.GetTable(typeof(CircularObjectA));
            var reader = new ObjectReader(table);

            var objA = new CircularObjectA { Id = 1, Name = "A", ObjectB = null };

            // Act
            var cells = reader.ReadCells(objA).ToList();

            // Assert
            Assert.NotNull(cells);
            Assert.True(cells.Count > 0, "Should return at least one cell");
        }

        [Fact]
        public void ReadCells_WithDeepNesting_ShouldCompleteSuccessfully()
        {
            // Arrange
            var registry = Translation;
            var table = registry.GetTable(typeof(CircularObjectA));
            var reader = new ObjectReader(table);

            var objA = new CircularObjectA { Id = 1, Name = "A" };
            var objB = new CircularObjectB { Id = 2, Name = "B" };
            objA.ObjectB = objB;
            objB.ObjectA = objA;

            // Act
            var cells = reader.ReadCells(objA).ToList();

            // Assert
            Assert.NotNull(cells);
        }

        private class CircularObjectA
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public CircularObjectB? ObjectB { get; set; }
        }

        private class CircularObjectB
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public CircularObjectA? ObjectA { get; set; }
        }
    }
}

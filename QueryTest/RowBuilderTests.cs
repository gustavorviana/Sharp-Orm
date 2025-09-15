using SharpOrm;

namespace QueryTest
{
    public class RowBuilderTests
    {
        private readonly RowBuilder _rowBuilder;

        public RowBuilderTests()
        {
            _rowBuilder = new RowBuilder();
        }

        [Fact]
        public void Add_WithKeyAndValue_ShouldAddCell()
        {
            // Arrange
            const string key = "TestKey";
            const string value = "TestValue";

            // Act
            var result = _rowBuilder.Add(key, value);

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.True(_rowBuilder.Contains(key));
            Assert.Equal(value, _rowBuilder[key]);
        }

        [Fact]
        public void Add_WithDuplicateKeyAndOverrideOnAddFalse_ShouldThrowArgumentException()
        {
            // Arrange
            const string key = "TestKey";
            _rowBuilder.Add(key, "FirstValue");
            _rowBuilder.OverrideOnAdd = false;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _rowBuilder.Add(key, "SecondValue"));
            Assert.Contains($"The key '{key}' already exists in the row.", ex.Message);
        }

        [Fact]
        public void Add_WithDuplicateKeyAndOverrideOnAddTrue_ShouldOverrideValue()
        {
            // Arrange
            const string key = "TestKey";
            const string firstValue = "FirstValue";
            const string secondValue = "SecondValue";
            _rowBuilder.Add(key, firstValue);
            _rowBuilder.OverrideOnAdd = true;

            // Act
            _rowBuilder.Add(key, secondValue);

            // Assert
            Assert.Equal(secondValue, _rowBuilder[key]);
        }

        [Fact]
        public void Indexer_WithNonExistentKey_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            const string nonExistentKey = "NonExistent";

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => { var value = _rowBuilder[nonExistentKey]; });
        }

        [Fact]
        public void Indexer_SetValue_ShouldAddOrUpdateCell()
        {
            // Arrange
            const string key = "TestKey";
            const string value = "TestValue";

            // Act
            _rowBuilder[key] = value;

            // Assert
            Assert.Equal(value, _rowBuilder[key]);
        }

        [Fact]
        public void Contains_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            const string key = "TestKey";
            _rowBuilder.Add(key, "TestValue");

            // Act
            var result = _rowBuilder.Contains(key);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Contains_WithNonExistentKey_ShouldReturnFalse()
        {
            // Arrange
            const string key = "NonExistentKey";

            // Act
            var result = _rowBuilder.Contains(key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Remove_WithExistingKey_ShouldRemoveAndReturnTrue()
        {
            // Arrange
            const string key = "TestKey";
            _rowBuilder.Add(key, "TestValue");

            // Act
            var result = _rowBuilder.Remove(key);

            // Assert
            Assert.True(result);
            Assert.False(_rowBuilder.Contains(key));
        }

        [Fact]
        public void Remove_WithNonExistentKey_ShouldReturnFalse()
        {
            // Arrange
            const string key = "NonExistentKey";

            // Act
            var result = _rowBuilder.Remove(key);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Reset_ShouldClearAllCells()
        {
            // Arrange
            _rowBuilder.Add("Key1", "Value1");
            _rowBuilder.Add("Key2", "Value2");

            // Act
            var result = _rowBuilder.Reset();

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.False(_rowBuilder.Contains("Key1"));
            Assert.False(_rowBuilder.Contains("Key2"));
        }

        [Fact]
        public void Add_WithCellArray_ShouldAddAllCells()
        {
            // Arrange
            var cells = new[]
            {
                new Cell("Key1", "Value1"),
                new Cell("Key2", "Value2")
            };

            // Act
            var result = _rowBuilder.Add(cells);

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.True(_rowBuilder.Contains("Key1"));
            Assert.True(_rowBuilder.Contains("Key2"));
            Assert.Equal("Value1", _rowBuilder["Key1"]);
            Assert.Equal("Value2", _rowBuilder["Key2"]);
        }

        [Fact]
        public void Add_WithPrefixAndCellArray_ShouldAddAllCellsWithPrefix()
        {
            // Arrange
            const string prefix = "Prefix_";
            var cells = new[]
            {
                new Cell("Key1", "Value1"),
                new Cell("Key2", "Value2")
            };

            // Act
            var result = _rowBuilder.Add(prefix, cells);

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.True(_rowBuilder.Contains($"{prefix}Key1"));
            Assert.True(_rowBuilder.Contains($"{prefix}Key2"));
            Assert.Equal("Value1", _rowBuilder[$"{prefix}Key1"]);
            Assert.Equal("Value2", _rowBuilder[$"{prefix}Key2"]);
        }

        [Fact]
        public void Add_WithRow_ShouldAddAllCellsFromRow()
        {
            // Arrange
            var cells = new[]
            {
                new Cell("Key1", "Value1"),
                new Cell("Key2", "Value2")
            };
            var row = new Row(cells);

            // Act
            var result = _rowBuilder.Add(row);

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.True(_rowBuilder.Contains("Key1"));
            Assert.True(_rowBuilder.Contains("Key2"));
        }

        [Fact]
        public void Add_WithPrefixAndRow_ShouldAddAllCellsFromRowWithPrefix()
        {
            // Arrange
            const string prefix = "Prefix_";
            var cells = new[]
            {
                new Cell("Key1", "Value1"),
                new Cell("Key2", "Value2")
            };
            var row = new Row(cells);

            // Act
            var result = _rowBuilder.Add(prefix, row);

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.True(_rowBuilder.Contains($"{prefix}Key1"));
            Assert.True(_rowBuilder.Contains($"{prefix}Key2"));
        }

        [Fact]
        public void GetCells_ShouldReturnAllCellsAsArray()
        {
            // Arrange
            _rowBuilder.Add("Key1", "Value1");
            _rowBuilder.Add("Key2", "Value2");

            // Act
            var cells = _rowBuilder.GetCells();

            // Assert
            Assert.Equal(2, cells.Length);
            Assert.Contains(cells, c => c.Name == "Key1" && c.Value.Equals("Value1"));
            Assert.Contains(cells, c => c.Name == "Key2" && c.Value.Equals("Value2"));
        }

        [Fact]
        public void GetRow_ShouldReturnRowWithAllCells()
        {
            // Arrange
            _rowBuilder.Add("Key1", "Value1");
            _rowBuilder.Add("Key2", "Value2");

            // Act
            var row = _rowBuilder.GetRow();

            // Assert
            Assert.Equal(2, row.Cells.Length);
            Assert.Contains(row.Cells, c => c.Name == "Key1");
            Assert.Contains(row.Cells, c => c.Name == "Key2");
        }

        [Fact]
        public void ImplicitConversion_ShouldConvertToRow()
        {
            // Arrange
            _rowBuilder.Add("Key1", "Value1");

            // Act
            Row row = _rowBuilder;

            // Assert
            Assert.NotNull(row);
            Assert.Single(row.Cells);
            Assert.Equal("Key1", row.Cells[0].Name);
        }

        [Theory]
        [InlineData("", "value")]
        [InlineData("  ", "value")]
        [InlineData("key", null)]
        [InlineData("key", "")]
        public void Add_WithVariousKeyValueCombinations_ShouldWork(string key, object value)
        {
            // Act
            var result = _rowBuilder.Add(key, value);

            // Assert
            Assert.Same(_rowBuilder, result);
            Assert.True(_rowBuilder.Contains(key));
            Assert.Equal(value, _rowBuilder[key]);
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public TestEntity2 Test { get; set; }
    }

    public class TestEntity2
    {
        public int Id { get; set; }
    }
}
using SharpOrm;
using System;
using System.Data;
using Xunit;

namespace QueryTest
{
    public class RowTests
    {
        [Fact]
        public void Indexer_WithNegativeIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(
                new Cell("Id", 1),
                new Cell("Name", "Test")
            );

            // Act & Assert
            var ex = Assert.Throws<IndexOutOfRangeException>(() => { var cell = row[-1]; });
            Assert.Contains("out of range", ex.Message);
        }

        [Fact]
        public void Indexer_WithIndexGreaterThanLength_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(
                new Cell("Id", 1),
                new Cell("Name", "Test")
            );

            // Act & Assert
            var ex = Assert.Throws<IndexOutOfRangeException>(() => { var cell = row[10]; });
            Assert.Contains("out of range", ex.Message);
        }

        [Fact]
        public void Indexer_WithValidIndex_ShouldReturnCell()
        {
            // Arrange
            var row = new Row(
                new Cell("Id", 1),
                new Cell("Name", "Test")
            );

            // Act
            var cell = row[0];

            // Assert
            Assert.NotNull(cell);
            Assert.Equal("Id", cell.Name);
            Assert.Equal(1, cell.Value);
        }

        [Fact]
        public void GetInt32_WithNegativeIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetInt32(-1));
        }

        [Fact]
        public void GetInt32_WithIndexGreaterThanLength_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetInt32(10));
        }

        [Fact]
        public void GetString_WithNegativeIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Name", "Test"));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetString(-1));
        }

        [Fact]
        public void GetString_WithIndexGreaterThanLength_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Name", "Test"));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetString(10));
        }

        [Fact]
        public void GetValue_WithNegativeIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetValue(-1));
        }

        [Fact]
        public void GetValue_WithIndexGreaterThanLength_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetValue(10));
        }

        [Fact]
        public void IsDBNull_WithNegativeIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.IsDBNull(-1));
        }

        [Fact]
        public void IsDBNull_WithIndexGreaterThanLength_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.IsDBNull(10));
        }

        [Fact]
        public void GetName_WithNegativeIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetName(-1));
        }

        [Fact]
        public void GetName_WithIndexGreaterThanLength_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => record.GetName(10));
        }

        [Fact]
        public void GetOrdinal_WithValidName_ShouldReturnIndex()
        {
            // Arrange
            var row = new Row(
                new Cell("Id", 1),
                new Cell("Name", "Test")
            );
            IDataRecord record = row;

            // Act
            var index = record.GetOrdinal("Name");

            // Assert
            Assert.Equal(1, index);
        }

        [Fact]
        public void GetOrdinal_WithInvalidName_ShouldReturnNegativeOne()
        {
            // Arrange
            var row = new Row(new Cell("Id", 1));
            IDataRecord record = row;

            // Act
            var index = record.GetOrdinal("NonExistent");

            // Assert
            Assert.Equal(-1, index);
        }
    }
}


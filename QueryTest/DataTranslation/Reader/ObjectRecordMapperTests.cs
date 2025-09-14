using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation.Reader;

namespace QueryTest.DataTranslation.Reader
{
    public class ObjectRecordMapperTests : DbMockTest
    {
        [Fact]
        public void MapSimpleObject_ShouldReadCorrectly()
        {
            // Arrange
            const uint id = 1;
            const string name = "Test Customer";
            const string email = "test@email.com";

            var reader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name),
                new Cell("Email", email)
            );

            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            var customer = Assert.IsType<CustomerWithoutAddress>(mapper.Current);
            Assert.Equal(id, customer.Id);
            Assert.Equal(name, customer.Name);
            Assert.Equal(email, customer.Email);
        }

        [Fact]
        public void MapObject_WithMissingColumn_ShouldIgnoreColumn()
        {
            // Arrange
            const uint id = 1;
            const string name = "Test Customer";
            // Email column is missing from reader

            var reader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name)
            );

            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            var customer = Assert.IsType<CustomerWithoutAddress>(mapper.Current);
            Assert.Equal(id, customer.Id);
            Assert.Equal(name, customer.Name);
            Assert.Null(customer.Email); // Should be null as column is missing
        }

        [Fact]
        public void MapObject_WithDbNullValues_ShouldHandleCorrectly()
        {
            // Arrange
            const uint id = 1;
            const string name = "Test Customer";

            var reader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name),
                new Cell("Email", DBNull.Value)
            );

            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            var customer = Assert.IsType<CustomerWithoutAddress>(mapper.Current);
            Assert.Equal(id, customer.Id);
            Assert.Equal(name, customer.Name);
            Assert.Null(customer.Email); // Should handle DBNull as null
        }

        [Fact]
        public void MoveNext_WithValidData_ShouldReturnTrueAndSetCurrent()
        {
            // Arrange
            var reader = GetReader(new Cell("Id", 1), new Cell("Name", "Test"));
            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            var result = mapper.MoveNext();

            // Assert
            Assert.True(result);
            Assert.NotNull(mapper.Current);
            Assert.IsType<CustomerWithoutAddress>(mapper.Current);
        }

        [Fact]
        public void MoveNext_WithNoData_ShouldReturnFalse()
        {
            // Arrange
            var reader = GetReader(); // Empty reader
            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            var result = mapper.MoveNext();

            // Assert
            Assert.False(result);
            Assert.Null(mapper.Current);
        }

        [Fact]
        public void Enumeration_ShouldWorkCorrectly()
        {
            // Arrange
            var reader = GetReader(
                (i) =>
                [
                    new Cell("Id", (uint)(i + 1)),
                    new Cell("Name", $"Customer {i + 1}"),
                    new Cell("Email", $"customer{i + 1}@email.com")
                ],
                3 // 3 rows
            );

            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            var results = new List<CustomerWithoutAddress>();
            foreach (CustomerWithoutAddress customer in mapper)
                results.Add(customer);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal(1u, results[0].Id);
            Assert.Equal("Customer 1", results[0].Name);
            Assert.Equal(2u, results[1].Id);
            Assert.Equal("Customer 2", results[1].Name);
            Assert.Equal(3u, results[2].Id);
            Assert.Equal("Customer 3", results[2].Name);
        }

        [Fact]
        public void Reset_ShouldThrowNotSupportedException()
        {
            // Arrange
            var reader = GetReader(new Cell("Id", 1));
            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerator)mapper).Reset());
        }

        [Fact]
        public void Constructor_WithNullReader_ShouldThrowArgumentNullException()
        {
            // Arrange
            var tableInfo = Translation.GetTable(typeof(CustomerWithoutAddress));

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ObjectRecordReader(null, tableInfo, null));
        }

        [Fact]
        public void MapComplexObject_WithMultipleNestedLevels_ShouldMapCorrectly()
        {
            // Arrange
            const int rootId = 1;
            const int child1Id = 2;
            const string child1Value = "Child1 Value";
            const int childChildId = 3;

            var reader = GetReader(
                new Cell("Id", rootId),
                new Cell("Child1_Id", child1Id),
                new Cell("Child1_Value", child1Value),
                new Cell("Child1_ChildId", childChildId)
            );

            Translation.NestedMapMode = SharpOrm.Builder.NestedMode.All;
            var tableInfo = Translation.GetTable(typeof(ComplexNestedObject));
            var mapper = new ObjectRecordReader(null, tableInfo, reader);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            var complex = Assert.IsType<ComplexNestedObject>(mapper.Current);
            Assert.Equal(rootId, complex.Id);
            Assert.NotNull(complex.Child1);
            Assert.Equal(child1Id, complex.Child1.Id);
            Assert.Equal(child1Value, complex.Child1.Value);
            Assert.Equal(childChildId, complex.Child1.ChildId);
        }

        #region Test Models
        public class ComplexNestedObject
        {
            public int Id { get; set; }
            public NestedChild Child1 { get; set; }
        }

        public class NestedChild
        {
            public int Id { get; set; }
            public string Value { get; set; }
            public int ChildId { get; set; }
        }
        #endregion
    }
}
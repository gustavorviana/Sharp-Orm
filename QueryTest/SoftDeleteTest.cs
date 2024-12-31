using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;

namespace QueryTest
{
    public class SoftDeleteTest : DbMockFallbackTest
    {
        [Theory]
        [InlineData(Trashed.With, "UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = @p1 WHERE [deleted] = 0")]
        [InlineData(Trashed.Only, "UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = @p1 WHERE [deleted] = 0")]
        [InlineData(Trashed.Except, "UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = @p1 WHERE [deleted] = 0")]
        public void DeleteWithDate(Trashed trashed, string expected)
        {
            using var fallback = this.RegisterFallback();
            using var query = new Query<SoftDeleteDateAddress> { Trashed = trashed };
            query.Delete();

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0")]
        [InlineData(Trashed.Only, "UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0")]
        [InlineData(Trashed.Except, "UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0")]
        public void Delete(Trashed trashed, string expected)
        {
            using var fallback = this.RegisterFallback();
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            query.Delete();

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "UPDATE [SoftDeleteAddress] SET [Name] = @p1")]
        [InlineData(Trashed.Only, "UPDATE [SoftDeleteAddress] SET [Name] = @p1 WHERE [deleted] = 1")]
        [InlineData(Trashed.Except, "UPDATE [SoftDeleteAddress] SET [Name] = @p1 WHERE [deleted] = 0")]
        public void Update(Trashed trashed, string expected)
        {
            using var fallback = this.RegisterFallback();
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            query.Update(new Cell("Name", "Test"));

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "SELECT * FROM [SoftDeleteAddress]")]
        [InlineData(Trashed.Only, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 1")]
        [InlineData(Trashed.Except, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 0")]
        public void Select(Trashed trashed, string expected)
        {
            using var fallback = this.RegisterFallback(new Cell("Id", 1));
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            query.Get();

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "SELECT * FROM [SoftDeleteAddress] WHERE [Street] != @p1")]
        [InlineData(Trashed.Only, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 1 AND ([Street] != @p1)")]
        [InlineData(Trashed.Except, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 0 AND ([Street] != @p1)")]
        public void SelectWhere(Trashed trashed, string expected)
        {
            using var fallback = this.RegisterFallback(new Cell("Id", 1));
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            query.WhereNot(x => x.Street, "First");
            query.Get();

            Assert.Equal(expected, fallback.ToString());
        }
    }
}

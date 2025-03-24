using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;

namespace QueryTest.Async
{
	public class SoftDeleteTests : DbMockFallbackTest
    {
		[Theory]
		[InlineData(Trashed.With, "UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = @p1 WHERE [deleted] = 0")]
		[InlineData(Trashed.Only, "UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = @p1 WHERE [deleted] = 0")]
		[InlineData(Trashed.Except, "UPDATE [SoftDeleteDateAddress] SET [deleted] = 1, [deleted_at] = @p1 WHERE [deleted] = 0")]
		public async Task DeleteWithDateAsync(Trashed trashed, string expected)
		{
			using var fallback = RegisterFallback();
			using var query = new Query<SoftDeleteDateAddress> { Trashed = trashed };
			await query.DeleteAsync(default);

			Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0")]
        [InlineData(Trashed.Only, "UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0")]
        [InlineData(Trashed.Except, "UPDATE [SoftDeleteAddress] SET [deleted] = 1 WHERE [deleted] = 0")]
        public async Task DeleteAsync(Trashed trashed, string expected)
        {
            using var fallback = RegisterFallback();
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            await query.DeleteAsync(default);

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "UPDATE [SoftDeleteAddress] SET [Name] = @p1")]
        [InlineData(Trashed.Only, "UPDATE [SoftDeleteAddress] SET [Name] = @p1 WHERE [deleted] = 1")]
        [InlineData(Trashed.Except, "UPDATE [SoftDeleteAddress] SET [Name] = @p1 WHERE [deleted] = 0")]
        public async Task UpdateAsync(Trashed trashed, string expected)
        {
            using var fallback = RegisterFallback();
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            await query.UpdateAsync(default, new Cell("Name", "Test"));

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "UPDATE [SoftDeleteAddress] SET [deleted] = 0 WHERE [deleted] = 1 AND ([Id] = 1)")]
        [InlineData(Trashed.Only, "UPDATE [SoftDeleteAddress] SET [deleted] = 0 WHERE [deleted] = 1 AND ([Id] = 1)")]
        [InlineData(Trashed.Except, "UPDATE [SoftDeleteAddress] SET [deleted] = 0 WHERE [deleted] = 1 AND ([Id] = 1)")]
        public async Task RestoreWhere(Trashed trashed, string expected)
        {
            using var fallback = RegisterFallback();
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            query.Where(x => x.Id, 1);
            await query.RestoreAsync(default);

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "SELECT * FROM [SoftDeleteAddress]")]
        [InlineData(Trashed.Only, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 1")]
        [InlineData(Trashed.Except, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 0")]
        public async Task SelectAsync(Trashed trashed, string expected)
        {
            using var fallback = RegisterFallback(new Cell("Id", 1));
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            await query.GetAsync(default);

            Assert.Equal(expected, fallback.ToString());
        }

        [Theory]
        [InlineData(Trashed.With, "SELECT * FROM [SoftDeleteAddress] WHERE [Street] != @p1")]
        [InlineData(Trashed.Only, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 1 AND ([Street] != @p1)")]
        [InlineData(Trashed.Except, "SELECT * FROM [SoftDeleteAddress] WHERE [deleted] = 0 AND ([Street] != @p1)")]
        public async Task SelectWhereAsync(Trashed trashed, string expected)
        {
            using var fallback = RegisterFallback(new Cell("Id", 1));
            using var query = new Query<SoftDeleteAddress> { Trashed = trashed };
            query.WhereNot(x => x.Street, "First");
            await query.GetAsync(default);

            QueryAssert.Equal(query, expected, fallback);
        }
    }
}

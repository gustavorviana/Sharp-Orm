using BaseTest.Utils;
using SharpOrm;

namespace QueryTest.Async
{
    public class QuestTests : DbMockFallbackTest
    {
        [Fact]
        public async Task SelectAsync()
        {
            var fallback = RegisterFallback();
            using var query = new Query("Customer");
            await query.ReadRowsAsync(default);

            Assert.Equal("SELECT * FROM [Customer]", fallback.ToString());
        }

        [Fact]
        public async Task UpdateAsync()
        {
            var fallback = RegisterFallback();
            using var query = new Query("Customer");
            await query.UpdateAsync(default, new Cell("Column", 1));

            Assert.Equal("UPDATE [Customer] SET [Column] = 1", fallback.ToString());
        }

        [Fact]
        public async Task DeleteAsync()
        {
            var fallback = RegisterFallback();
            using var query = new Query("Customer");
            await query.DeleteAsync(default);

            Assert.Equal("DELETE FROM [Customer]", fallback.ToString());
        }

        [Fact]
        public async Task Insert()
        {
            var fallback = RegisterFallback();
            using var query = new Query("Customer");
            await query.InsertAsync(default, new Cell("Column", 1));

            Assert.Equal("INSERT INTO [Customer] ([Column]) VALUES (1); SELECT SCOPE_IDENTITY();", fallback.ToString());
        }

        [Fact]
        public async Task FirstRowAsync()
        {
            var fallback = RegisterFallback();
            using var query = new Query("Customer");
            await query.FirstRowAsync(default);

            Assert.Equal("SELECT TOP(1) * FROM [Customer]", fallback.ToString());
        }

        [Fact]
        public async Task UpsertAsyncAsync()
        {
            var fallback = RegisterFallback();
            using var query = new Query("Customer");
            await query.UpsertAsync(new Row(new Cell("Key", 1), new Cell("Value", 2)), ["Key"]);

            Assert.Equal("MERGE INTO [Customer] [Target] USING(VALUES (1, 2)) AS [Source] ([Key], [Value]) ON [Source].[Key]=[Target].[Key] WHEN MATCHED THEN UPDATE SET [Target].[Value]=[Source].[Value] WHEN NOT MATCHED THEN INSERT ([Key], [Value]) VALUES ([Source].[Key], [Source].[Value]);", fallback.ToString());
        }
    }
}

using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;

namespace QueryTest
{
    public class TimestampsTest : DbMockFallbackTest
    {
        [Fact]
        public void InsertTestTest()
        {
            using var fallback = RegisterFallback();
            using var query = new Query<AddressWithTimeStamp>();
            query.Insert(new AddressWithTimeStamp(1) { Name = "Test" });

            Assert.Equal("INSERT INTO [AddressWithTimeStamp] ([Id], [Name], [Street], [City], [CreatedAt], [UpdatedAt]) VALUES (1, @p1, NULL, NULL, @p2, @p3)", fallback.ToString());
            var dbParams = fallback.GetParameters();

            Assert.IsType<DateTime>(dbParams[1].Value);
            Assert.IsType<DateTime>(dbParams[2].Value);
        }

        [Fact]
        public void UpdateTestTest()
        {
            var updateTime = DateTime.UtcNow.AddDays(1);

            using var fallback = RegisterFallback();
            using var query = new Query<AddressWithTimeStamp>();
            query.Where(x => x.Id, 1);
            query.Update(new AddressWithTimeStamp(1) { Name = "Test" });

            Assert.Equal("UPDATE [AddressWithTimeStamp] SET [Name] = @p1, [Street] = NULL, [City] = NULL, [UpdatedAt] = @p2 WHERE [Id] = 1", fallback.ToString());
            var dbParams = fallback.GetParameters();

            Assert.IsType<DateTime>(dbParams[1].Value);
        }

        [Fact]
        public void MappedWithUpdatedOnlyInsert()
        {
            var translation = new TranslationRegistry();
            var mapper = new TableMap<Address>(translation);
            mapper.HasTimeStamps(null, "UpdatedAt");
            mapper.HasKey(x => x.Id).Build();

            using var fallback = RegisterFallback();
            using var query = new Query<Address>(GetManager(translation));
            query.Insert(new Address(1) { Name = "Test" });

            Assert.Equal("INSERT INTO [Address] ([Id], [Name], [Street], [City], [UpdatedAt]) VALUES (1, @p1, NULL, NULL, @p2)", fallback.ToString());
            var dbParams = fallback.GetParameters();

            Assert.IsType<DateTime>(dbParams[1].Value);
        }

        [Fact]
        public void MappedWithCreateOnlyUpdate()
        {
            var translation = new TranslationRegistry();
            var mapper = new TableMap<Address>(translation);
            mapper.HasTimeStamps("CreatedAt", null);
            mapper.HasKey(x => x.Id).Build();

            using var fallback = RegisterFallback();
            using var query = new Query<Address>(GetManager(translation));
            query.Where(x => x.Id, 1);
            query.Update(new Address(1) { Name = "Test" });

            Assert.Equal("UPDATE [Address] SET [Name] = @p1, [Street] = NULL, [City] = NULL WHERE [Id] = 1", fallback.ToString());
        }
    }
}

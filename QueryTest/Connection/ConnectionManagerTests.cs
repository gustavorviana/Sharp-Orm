using BaseTest.Utils;
using SharpOrm.Builder;
using SharpOrm.Connection;
using SharpOrm.Errors;

namespace QueryTest.Connection;

public class ConnectionManagerTests : DbMockFallbackTest
{
    [Fact]
    public void ManagerErrorsTest()
    {
        const string Error = "Test Error";
        using (RegisterFallback(x => throw new DatabaseException(Error)))
        {
            Exception? exception = null;
            Manager.OnError += (handler, args) => exception = args.Exception;

            Assert.Throws<DatabaseException>(() => Manager.ExecuteNonQuery("SELECT ERROR"));
            var validatedException = Assert.IsType<DatabaseException>(exception);
            Assert.Equal(Error, validatedException.Message);
        }
    }

    [Fact]
    public void CreateCommandBuilderTest()
    {
        const int EXPECTED_INITIAL_TIMEOUT = 30;
        const int EXPECTED_TIMEOUT = 120;

        var initialTimeout = Manager.CreateCommand().CommandTimeout;

        Manager.CommandTimeout = EXPECTED_TIMEOUT;
        var timeout = Manager.CreateCommand().CommandTimeout;

        Assert.Equal(EXPECTED_INITIAL_TIMEOUT, initialTimeout);
        Assert.Equal(EXPECTED_TIMEOUT, timeout);
    }
    [Fact]
    public void GetQuery_ShouldReturnNonNullAndSameManager()
    {
        // Act
        var query = Manager.GetQuery("Query");
        var queryWithTableName = Manager.GetQuery(new DbName("Table"));

        var genericQuery = Manager.GetQuery<object>();
        var genericQueryWithConfig = Manager.GetQuery<object>("Alias");
        var genericQueryWithTableName = Manager.GetQuery<object>(new DbName("Table"));

        // Assert
        Assert.NotNull(query);
        Assert.NotNull(genericQuery);
        Assert.NotNull(genericQueryWithConfig);
        Assert.NotNull(queryWithTableName);
        Assert.NotNull(genericQueryWithTableName);

        Assert.Same(Manager, query.Manager);
        Assert.Same(Manager, genericQuery.Manager);
        Assert.Same(Manager, genericQueryWithConfig.Manager);
        Assert.Same(Manager, queryWithTableName.Manager);
        Assert.Same(Manager, genericQueryWithTableName.Manager);
    }
}

using BaseTest.Utils;
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
}

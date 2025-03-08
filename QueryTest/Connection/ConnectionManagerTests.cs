using BaseTest.Utils;
using SharpOrm.Connection;
using SharpOrm.Errors;

namespace QueryTest.Connection;

public class ConnectionManagerTests : DbMockFallbackTest
{
    [Fact]
    public void ManagerErrors()
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
}

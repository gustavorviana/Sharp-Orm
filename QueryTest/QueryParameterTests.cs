using BaseTest.Mock;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using System.Data.SqlClient;

namespace QueryTest
{
    public class QueryParameterTests : DbMockTest
    {
        [Fact]
        public void InitParamTest()
        {
            // Arrange
            var param = new QueryParam("Name", "Test");
            var cmd = new MockCommand();

            // Act
            param.Init(cmd);

            // Assert
            var dbParam = Assert.Single(cmd.Parameters) as SqlParameter;

            Assert.NotNull(dbParam);
            Assert.Equal("@Name", dbParam.ParameterName);
            Assert.Equal("Test", dbParam.Value);
        }

        [Fact]
        public void InitManyTimesMultipleParams()
        {
            // Arrange
            var param = new QueryParam("Name", "Test");
            var cmd = new MockCommand();

            // Act
            param.Init(cmd);
            param.Init(cmd);
            param.Init(cmd);
            param.Init(cmd);

            // Assert
            var dbParam = Assert.Single(cmd.Parameters) as SqlParameter;

            Assert.NotNull(dbParam);
            Assert.Equal("@Name", dbParam.ParameterName);
            Assert.Equal("Test", dbParam.Value);
        }

        [Fact]
        public void AddToQueryBuilder()
        {
            // Arrange
            var builder = new QueryBuilder(new QueryInfo(Config, new DbName("Test", "", false)));
            var nameParam = new QueryParam("Name", "Test");
            var parentParam = new QueryParam("ParentName", "Parent Test");
            var cmd = new MockCommand();

            // Act
            builder.Add(new SqlExpression("Name=? OR Alias=? OR Parent=?", nameParam, nameParam, parentParam));
            cmd.SetExpression(builder.ToExpression());

            // Assert
            Assert.Equal(2, cmd.Parameters.Count);

            Assert.Equal("@Name", ((SqlParameter)cmd.Parameters[0]).ParameterName);
            Assert.Equal("Test", ((SqlParameter)cmd.Parameters[0]).Value);

            Assert.Equal("@ParentName", ((SqlParameter)cmd.Parameters[1]).ParameterName);
            Assert.Equal("Parent Test", ((SqlParameter)cmd.Parameters[1]).Value);
        }
    }
}

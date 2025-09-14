using BaseTest.Mock;
using BaseTest.Utils;
using NSubstitute;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace QueryTest
{
    public class DbParameterCreationTests : DbMockTest
    {
        private readonly QueryConfig mockConfig;
        private readonly MockCommand mockCommand;

        public DbParameterCreationTests()
        {
            mockConfig = Substitute.For<QueryConfig>();
            mockConfig.CreateParameterName(Arg.Any<int>()).Returns(callInfo => $"@param{callInfo.Arg<int>()}");
            mockCommand = new MockCommand();
        }

        [Fact]
        public void QueryParam_Init_CreatesDbParameterWithCorrectName()
        {
            // Arrange
            var queryParam = new QueryParam("TestParam", "TestValue");

            // Act
            var dbParam = queryParam.Init(mockCommand);

            // Assert
            Assert.Equal("@TestParam", dbParam.ParameterName);
            Assert.Equal("TestValue", dbParam.Value);
            Assert.Single(mockCommand.Parameters);
        }

        [Fact]
        public void QueryParam_Init_WithExistingParameter_ReturnsExisting()
        {
            // Arrange
            var queryParam = new QueryParam("TestParam", "TestValue");

            // First initialization
            queryParam.Init(mockCommand);
            var firstParamCount = mockCommand.Parameters.Count;

            // Act - Second initialization
            var dbParam = queryParam.Init(mockCommand);

            // Assert
            Assert.Equal(firstParamCount, mockCommand.Parameters.Count); // Should not add duplicate
            Assert.Equal("@TestParam", dbParam.ParameterName);
        }

        [Fact]
        public void QueryParam_Init_NullCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var queryParam = new QueryParam("TestParam", "TestValue");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => queryParam.Init(null));
        }

        [Fact]
        public void ConnectionUtils_AddParam_WithQueryParam_CallsInit()
        {
            // Arrange
            var queryParam = new QueryParam("TestParam", "TestValue");

            // Act
            var dbParam = mockCommand.AddParam("@TestParam", queryParam);

            // Assert
            Assert.Equal("@TestParam", dbParam.ParameterName);
            Assert.Equal("TestValue", dbParam.Value);
            Assert.Single(mockCommand.Parameters);
        }

        [Fact]
        public void ConnectionUtils_AddParam_WithRegularValue_CreatesParameter()
        {
            // Act
            var dbParam = mockCommand.AddParam("@RegularParam", "RegularValue");

            // Assert
            Assert.Equal("@RegularParam", dbParam.ParameterName);
            Assert.Equal("RegularValue", dbParam.Value);
            Assert.Single(mockCommand.Parameters);
        }

        [Fact]
        public void ConnectionUtils_AddParam_WithNullValue_UsesDBNull()
        {
            // Act
            var dbParam = mockCommand.AddParam("@NullParam", null);

            // Assert
            Assert.Equal("@NullParam", dbParam.ParameterName);
            Assert.Equal(DBNull.Value, dbParam.Value);
            Assert.Single(mockCommand.Parameters);
        }

        [Fact]
        public void DbCommandExtension_SetExpression_WithConfig_CreatesParametersCorrectly()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Name = ? AND Age = ?", "John", 25);

            // Act
            mockCommand.SetExpression(mockConfig, expression);

            // Assert
            Assert.Equal(2, mockCommand.Parameters.Count);

            var param1 = (SqlParameter)mockCommand.Parameters[0];
            var param2 = (SqlParameter)mockCommand.Parameters[1];

            Assert.Equal("@param1", param1.ParameterName);
            Assert.Equal("John", param1.Value);

            Assert.Equal("@param2", param2.ParameterName);
            Assert.Equal(25, param2.Value);
        }

        [Fact]
        public void DbCommandExtension_SetExpression_ObsoleteMethod_CreatesParametersCorrectly()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Name = ? AND Age = ?", "John", 25);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            mockCommand.SetExpression(expression);
#pragma warning restore CS0618

            // Assert
            Assert.Equal(2, mockCommand.Parameters.Count);

            var param1 = (SqlParameter)mockCommand.Parameters[0];
            var param2 = (SqlParameter)mockCommand.Parameters[1];

            Assert.Equal("@p1", param1.ParameterName);
            Assert.Equal("John", param1.Value);

            Assert.Equal("@p2", param2.ParameterName);
            Assert.Equal(25, param2.Value);
        }

        [Fact]
        public void DbCommandExtension_SetExpression_WithQueryParam_UsesQueryParamName()
        {
            // Arrange
            var queryParam = new QueryParam("CustomName", "CustomValue");
            var expression = new SqlExpression("SELECT * FROM Users WHERE Name = ?", queryParam);

            // Act
            mockCommand.SetExpression(mockConfig, expression);

            // Assert
            var dbParam = Assert.Single(mockCommand.Parameters) as SqlParameter;
            Assert.Equal("@CustomName", dbParam.ParameterName);
            Assert.Equal("CustomValue", dbParam.Value);
        }

        [Fact]
        public void SqlExpression_GetParamName_WithQueryParam_ReturnsQueryParamName()
        {
            // Arrange
            var queryParam = new QueryParam("SpecialParam", "SpecialValue");
            var expression = new SqlExpression("SELECT * FROM Users WHERE Id = ?", queryParam);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var paramName = expression.GetParamName(1);
#pragma warning restore CS0618

            // Assert
            Assert.Equal("@SpecialParam", paramName);
        }

        [Fact]
        public void SqlExpression_GetParamName_WithRegularParam_ReturnsDefaultName()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Id = ?", 123);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var paramName = expression.GetParamName(1);
#pragma warning restore CS0618

            // Assert
            Assert.Equal("@p1", paramName);
        }

        [Fact]
        public void QueryBuilder_AddParameter_WithQueryParam_DoesNotCallCreateParameterName()
        {
            // Arrange
            var queryInfo = new ReadonlyQueryInfo(mockConfig, new DbName("TestTable"));
            var builder = new QueryBuilder(queryInfo);
            var queryParam = new QueryParam("ExistingParam", "ExistingValue");

            // Act
            builder.AddParameter(queryParam);

            // Assert
            mockConfig.DidNotReceive().CreateParameterName(Arg.Any<int>());
        }

        [Fact]
        public void QueryParam_Constructor_WithoutAtSymbol_AddsAtSymbol()
        {
            // Arrange & Act
            var queryParam = new QueryParam("ParamName", "ParamValue");

            // Assert
            Assert.Equal("@ParamName", queryParam.Name);
        }

        [Fact]
        public void QueryParam_Constructor_WithAtSymbol_KeepsAtSymbol()
        {
            // Arrange & Act
            var queryParam = new QueryParam("@ParamName", "ParamValue");

            // Assert
            Assert.Equal("@ParamName", queryParam.Name);
        }

        [Fact]
        public void QueryParam_OutputParameter_StoresReference()
        {
            // Arrange
            var queryParam = new QueryParam("OutputParam", "InitialValue", ParameterDirection.Output);

            // Act
            var dbParam = queryParam.Init(mockCommand);

            // Assert
            Assert.Equal(ParameterDirection.Output, dbParam.Direction);
            Assert.Equal("@OutputParam", dbParam.ParameterName);
        }

        [Fact]
        public void QueryParam_InputOutputParameter_StoresReference()
        {
            // Arrange
            var queryParam = new QueryParam("InputOutputParam", "InitialValue", ParameterDirection.InputOutput);

            // Act
            var dbParam = queryParam.Init(mockCommand);

            // Assert
            Assert.Equal(ParameterDirection.InputOutput, dbParam.Direction);
            Assert.Equal("@InputOutputParam", dbParam.ParameterName);
        }

        [Fact]
        public void DbCommandExtension_CreateCommand_WithConfig_CreatesParametersCorrectly()
        {
            // Arrange
            var mockConnection = Substitute.For<DbConnection>();
            mockConnection.CreateCommand().Returns(new MockCommand());

            // Act
            var command = mockConnection.CreateCommand(mockConfig, "SELECT * FROM Users WHERE Id = ? AND Name = ?", 123, "John");

            // Assert
            Assert.NotNull(command);
            Assert.Single(command.Parameters);

            var param1 = (SqlParameter)command.Parameters[0];

            Assert.Equal("@param1", param1.ParameterName);
            Assert.Equal("John", param1.Value);
        }

        [Fact]
        public void DbCommandExtension_CreateCommand_ObsoleteMethod_CreatesParametersCorrectly()
        {
            // Arrange
            var mockConnection = Substitute.For<DbConnection>();
            mockConnection.CreateCommand().Returns(new MockCommand());

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var command = mockConnection.CreateCommand("SELECT * FROM Users WHERE Id = ? AND Name = ?", 123, "John");
#pragma warning restore CS0618

            // Assert
            Assert.NotNull(command);
            Assert.Single(command.Parameters);

            var param1 = (SqlParameter)command.Parameters[0];

            Assert.Equal("@p1", param1.ParameterName);
            Assert.Equal("John", param1.Value);
        }

        [Fact]
        public void DbCommandExtension_SetQuery_WithConfig_CreatesParametersCorrectly()
        {
            // Act
            mockCommand.SetQuery(mockConfig, "SELECT * FROM Users WHERE Id = ? AND Name = ?", 123, "John");

            // Assert
            Assert.Single(mockCommand.Parameters);

            var param1 = (SqlParameter)mockCommand.Parameters[0];

            Assert.Equal("@param1", param1.ParameterName);
            Assert.Equal("John", param1.Value);
        }

        [Fact]
        public void DbCommandExtension_SetQuery_ObsoleteMethod_CreatesParametersCorrectly()
        {
            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            mockCommand.SetQuery("SELECT * FROM Users WHERE Id = ? AND Name = ?", 123, "John");
#pragma warning restore CS0618

            // Assert
            Assert.Single(mockCommand.Parameters);

            var param1 = (SqlParameter)mockCommand.Parameters[0];

            Assert.Equal("@p1", param1.ParameterName);
            Assert.Equal("John", param1.Value);
        }

        [Fact]
        public void DecodeExpressionString_WithConfig_ReplacesQuestionMarks()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Name = ? AND Age = ?", "John", 25);

            // Act
            var decodedString = DbCommandExtension.DecodeExpressionString(expression, mockConfig);

            // Assert
            Assert.Equal("SELECT * FROM Users WHERE Name = @param1 AND Age = @param2", decodedString);
        }

        [Fact]
        public void DecodeExpressionString_ObsoleteMethod_ReplacesQuestionMarks()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Name = ? AND Age = ?", "John", 25);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var decodedString = DbCommandExtension.DecodeExpressionString(expression);
#pragma warning restore CS0618

            // Assert
            Assert.Equal("SELECT * FROM Users WHERE Name = @p1 AND Age = @p2", decodedString);
        }

        [Fact]
        public void GetParameterName_WithQueryParam_ReturnsQueryParamName()
        {
            // Arrange
            var queryParam = new QueryParam("CustomParam", "CustomValue");
            var expression = new SqlExpression("SELECT * FROM Users WHERE Id = ?", queryParam);

            // Act
            // Using reflection to access private method for testing
            var method = typeof(DbCommandExtension).GetMethod("GetParameterName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paramName = (string)method.Invoke(null, new object[] { expression, 0, mockConfig });

            // Assert
            Assert.Equal("@CustomParam", paramName);
        }

        [Fact]
        public void GetParameterName_WithConfig_ReturnsConfigParameterName()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Id = ?", 123);

            // Act
            // Using reflection to access private method for testing
            var method = typeof(DbCommandExtension).GetMethod("GetParameterName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paramName = (string)method.Invoke(null, new object[] { expression, 0, mockConfig });

            // Assert
            Assert.Equal("@param1", paramName);
        }

        [Fact]
        public void GetParameterName_WithoutConfig_UsesFallback()
        {
            // Arrange
            var expression = new SqlExpression("SELECT * FROM Users WHERE Id = ?", 123);

            // Act
            // Using reflection to access private method for testing
            var method = typeof(DbCommandExtension).GetMethod("GetParameterName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paramName = (string)method.Invoke(null, new object[] { expression, 0, null });

            // Assert
            Assert.Equal("@p1", paramName);
        }
    }
}
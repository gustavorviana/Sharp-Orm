using SharpOrm;
using System.Text;

namespace QueryTest
{
    public class SqlExpressionTests
    {
        [Fact]
        public void ValidateParametersTest()
        {
            var exp = new SqlExpression("Name=? OR Alias=? OR Parent=?", "Test", "Test", "Parent Test");

            Assert.Equal(3, exp.Parameters.Length);
            Assert.Equal("Test", exp.Parameters[0]);
            Assert.Equal("Test", exp.Parameters[1]);
            Assert.Equal("Parent Test", exp.Parameters[2]);
        }

        [Fact]
        public void IsEmptyTest()
        {
            Assert.False(new SqlExpression("Name").IsEmpty);
            Assert.True(new SqlExpression("").IsEmpty);
        }

        [Fact]
        public void IsEmptyBatchSqlExpressionTest()
        {
            Assert.False(new BatchSqlExpression(new SqlExpression("Name")).IsEmpty);
            Assert.True(new BatchSqlExpression().IsEmpty);
        }

        [Fact]
        public void BatchToStringTest()
        {
            var exp = new BatchSqlExpression(new SqlExpression("SCRIPT 1;"), new SqlExpression("SCRIPT 2"));

            StringBuilder builder = new StringBuilder();
            builder
                .AppendLine("SCRIPT 1;")
                .AppendLine("\\")
                .AppendLine("SCRIPT 2");

            Assert.Equal(builder.ToString(), exp.ToString());
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("Name", new SqlExpression("Name").ToString());
        }
    }
}

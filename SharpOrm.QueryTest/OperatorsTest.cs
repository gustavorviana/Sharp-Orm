using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Operators;

namespace UnityTest
{
    public class OperatorsTest
    {
        private static readonly ReadonlyQueryInfo config = new(new ReadonlyQueryConfig(), new DbName("Table"));

        [Fact]
        public void CoalesceColumnsTest()
        {
            var op = new Coalesce(new Column("Table.FirstName"), new Column("Table.LastName"), new Column(new ValueExp("No Name")));
            var expected = new SqlExpression("COALESCE(Table.FirstName,Table.LastName,?)", "No Name");
            Assert.Equal(expected, op.ToSafeExpression(config, false));
        }

        [Fact]
        public void CoalesceExpressions()
        {
            var op = new Coalesce((Column)"Table.FirstName", (Column)"TRIM(Table.LastName)");
            var expected = new SqlExpression("COALESCE(Table.FirstName,TRIM(Table.LastName))");
            Assert.Equal(expected, op.ToSafeExpression(config, false));
        }

        [Fact]
        public void CoalesceAliasTest()
        {
            var op = new Coalesce((Column)"Table.FirstName", (Column)"TRIM(Table.LastName)") { Alias = "Name" };
            var expected = new SqlExpression("COALESCE(Table.FirstName,TRIM(Table.LastName)) Name");
            Assert.Equal(expected, op.ToSafeExpression(config, false));
        }

        [Fact]
        public void Avg()
        {
            Assert.Equal(new SqlExpression("AVG(Column)"), new Avg("Column").ToSafeExpression(config, false));
            Assert.Equal(new SqlExpression("AVG(Column) myCol"), new Avg("Column", "myCol").ToSafeExpression(config, true));
        }

        [Fact]
        public void Count()
        {
            Assert.Equal(new SqlExpression("COUNT(Column)"), new Count("Column").ToSafeExpression(config, false));
            Assert.Equal(new SqlExpression("COUNT(Column) myCol"), new Count("Column", "myCol").ToSafeExpression(config, true));
        }

        [Fact]
        public void Max()
        {
            Assert.Equal(new SqlExpression("MAX(Column)"), new Max("Column").ToSafeExpression(config, false));
            Assert.Equal(new SqlExpression("MAX(Column) myCol"), new Max("Column", "myCol").ToSafeExpression(config, true));
        }

        [Fact]
        public void Min()
        {
            Assert.Equal(new SqlExpression("MIN(Column)"), new Min("Column").ToSafeExpression(config, false));
            Assert.Equal(new SqlExpression("MIN(Column) myCol"), new Min("Column", "myCol").ToSafeExpression(config, true));
        }

        [Fact]
        public void Sum()
        {
            Assert.Equal(new SqlExpression("SUM(Column)"), new Sum("Column").ToSafeExpression(config, false));
            Assert.Equal(new SqlExpression("SUM(Column) myCol"), new Sum("Column", "myCol").ToSafeExpression(config, true));
        }
    }
}

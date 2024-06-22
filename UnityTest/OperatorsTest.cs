using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Operators;

namespace UnityTest
{
    [TestClass]
    public class OperatorsTest
    {
        private static readonly ReadonlyQueryInfo config = new(new ReadonlyQueryConfig(), new DbName("Table"));

        [TestMethod]
        public void CoalesceColumnsTest()
        {
            var op = new Coalesce(new Column("Table.FirstName"), new Column("Table.LastName"), new Column(new ValueExp("No Name")));
            var expected = new SqlExpression("COALESCE(Table.FirstName,Table.LastName,?)", "No Name");
            Assert.AreEqual(expected, op.ToSafeExpression(config, false));
        }

        [TestMethod]
        public void CoalesceExpressions()
        {
            var op = new Coalesce((Column)"Table.FirstName", (Column)"TRIM(Table.LastName)");
            var expected = new SqlExpression("COALESCE(Table.FirstName,TRIM(Table.LastName))");
            Assert.AreEqual(expected, op.ToSafeExpression(config, false));
        }

        [TestMethod]
        public void CoalesceAliasTest()
        {
            var op = new Coalesce((Column)"Table.FirstName", (Column)"TRIM(Table.LastName)") { Alias = "Name" };
            var expected = new SqlExpression("COALESCE(Table.FirstName,TRIM(Table.LastName)) Name");
            Assert.AreEqual(expected, op.ToSafeExpression(config, false));
        }

        [TestMethod]
        public void Avg()
        {
            Assert.AreEqual(new SqlExpression("AVG(Column)"), new Avg("Column").ToSafeExpression(config, false));
            Assert.AreEqual(new SqlExpression("AVG(Column) myCol"), new Avg("Column", "myCol").ToSafeExpression(config, true));
        }

        [TestMethod]
        public void Count()
        {
            Assert.AreEqual(new SqlExpression("COUNT(Column)"), new Count("Column").ToSafeExpression(config, false));
            Assert.AreEqual(new SqlExpression("COUNT(Column) myCol"), new Count("Column", "myCol").ToSafeExpression(config, true));
        }

        [TestMethod]
        public void Max()
        {
            Assert.AreEqual(new SqlExpression("MAX(Column)"), new Max("Column").ToSafeExpression(config, false));
            Assert.AreEqual(new SqlExpression("MAX(Column) myCol"), new Max("Column", "myCol").ToSafeExpression(config, true));
        }

        [TestMethod]
        public void Min()
        {
            Assert.AreEqual(new SqlExpression("MIN(Column)"), new Min("Column").ToSafeExpression(config, false));
            Assert.AreEqual(new SqlExpression("MIN(Column) myCol"), new Min("Column", "myCol").ToSafeExpression(config, true));
        }

        [TestMethod]
        public void Sum()
        {
            Assert.AreEqual(new SqlExpression("SUM(Column)"), new Sum("Column").ToSafeExpression(config, false));
            Assert.AreEqual(new SqlExpression("SUM(Column) myCol"), new Sum("Column", "myCol").ToSafeExpression(config, true));
        }
    }
}

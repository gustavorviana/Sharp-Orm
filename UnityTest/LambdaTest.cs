using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Linq;
using System.Linq.Expressions;
using UnityTest.Models;

namespace UnityTest
{
    [TestClass]
    public class LambdaTest
    {
        private static readonly QueryInfo Info = new(new MysqlQueryConfig(false), new DbName("MyTable"));

        [TestMethod]
        public void GetOneColumn()
        {
            var columns = ToColumn(c => c.Customer);

            Assert.AreEqual(1, columns.Length);
            Assert.AreEqual(typeof(Customer), columns[0].ValueType);
        }

        [TestMethod]
        public void GetMultiple()
        {
            var columns = ToColumn(c => c.Customer.Address);

            Assert.AreEqual(2, columns.Length);
            Assert.AreEqual(typeof(Customer), columns[0].ValueType);
            Assert.AreEqual(typeof(Address), columns[1].ValueType);
        }

        private static LambdaColumn[] ToColumn(Expression<ColumnExpression<Order>> check)
        {
            return new ColumnExpressionVisitor().VisitColumn(check).ToArray();
        }

        [TestMethod]
        public void CompareWithString()
        {
            SqlExpression expected = new("(`Name` = ?)", "my name");
            Assert.AreEqual(expected, ToExpression(c => c.Name == "my name"));
        }

        [TestMethod]
        public void CompareWithConst()
        {
            const string MyConst = "my name";
            SqlExpression expected = new("(`Name` = ?)", MyConst);
            Assert.AreEqual(expected, ToExpression(c => c.Name == MyConst));
        }

        [TestMethod]
        public void CompareWithClassField()
        {
            SqlExpression expected = new("(`Name` = `Value`)");
            Assert.AreEqual(expected, ToExpression(c => c.Name == c.Value));
        }

        private static SqlExpression ToExpression(Expression<Func<Test, bool>> check)
        {
            return SqlLambdaVisitor.ParseLambda(Info, check);
        }

        private class Test
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public int Index { get; set; }
        }
    }
}

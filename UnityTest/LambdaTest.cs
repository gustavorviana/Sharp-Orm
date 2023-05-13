using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.Linq.Expressions;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class LambdaTest : BaseTest
    {
        private static readonly QueryInfo Info = new(new MysqlQueryConfig(false));

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

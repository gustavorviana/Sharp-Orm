using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace UnityTest.Utils
{
    internal class TestAssert
    {
        public static void AreEqualsDate(DateTime expected, object actual, string message)
        {
            Assert.IsInstanceOfType(actual, typeof(DateTime));
            AreEqualsDate(expected, (DateTime)actual, message);
        }

        public static void AreEqualsDate(DateTime expected, DateTime actual, string message)
        {
            Assert.AreEqual(expected.Date, actual.Date);
            try
            {
                Assert.AreEqual(decimal.Truncate((decimal)expected.TimeOfDay.TotalSeconds), decimal.Truncate((decimal)actual.TimeOfDay.TotalSeconds), message);
            }
            catch
            {
                Console.WriteLine(expected);
                Console.WriteLine(actual);
                throw;
            }
        }

        public static void AreEqual(SqlExpression expected, SqlExpression actual)
        {
            Assert.AreEqual(expected.ToString(), actual.ToString(), "The SQL expressions do not match.");
            CollectionAssert.AreEqual(expected.Parameters, actual.Parameters, "The arguments of the expression do not match.");
        }

        public static void AreDecoded(string expected, SqlExpression actual)
        {
            Assert.AreEqual(expected, DbCommandExtension.DecodeExpressionString(actual));
        }

        public static void AreEqualsParameters(SqlExpression exp, params int[] paramIndexes)
        {
            if (paramIndexes.Length == 0)
                throw new ArgumentNullException(nameof(paramIndexes));

            using var cmd = new SqlCommand();
            cmd.SetExpression(exp);

            foreach (var index in paramIndexes)
                AreValidParam(cmd, index, cmd.Parameters[index].Value);
        }

        public static void TestExpectedSelectExpression(Query query, string expected, SqlExpression expression, object[] expectedValues)
        {
            query.Where(expression);
            TestExpected(query.Info.Config.NewGrammar(query).Select(), expected, expectedValues);
        }

        public static void TestExpected(SqlExpression exp, string expectedSql, object[] expectedValues)
        {
            using var command = new SqlCommand();
            command.SetExpression(exp);

            Assert.AreEqual(expectedSql, command.CommandText);
            Assert.AreEqual(expectedValues.Length, command.Parameters.Count);
            var dbParams = command.Parameters.OfType<DbParameter>().ToArray();

            for (int i = 0; i < dbParams.Length; i++)
                AreValidParam(command, i, expectedValues[i]);
        }

        public static void AreValidParam(DbCommand cmd, int index, object value)
        {
            var param = cmd.Parameters[index];
            Assert.AreEqual(DbCommandExtension.GetParamName(index + 1), param.ParameterName);

            if (value == null || value is DBNull) Assert.IsTrue(param.Value is DBNull);
            else Assert.AreEqual(value, param.Value);
        }
    }
}

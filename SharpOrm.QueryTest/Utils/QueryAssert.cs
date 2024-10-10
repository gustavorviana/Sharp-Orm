using SharpOrm;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;

namespace QueryTest.Utils
{
    internal static class QueryAssert
    {
        #region Obsoletes
        [Obsolete("This is an override of Object.Equals(). Call Assert.Equals() instead.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static bool Equals(object a, object b)
        {
            throw new InvalidOperationException("Assert.Equals should not be used");
        }

        /// <summary>Do not call this method.</summary>
        [Obsolete("This is an override of Object.ReferenceEquals(). Call Assert.Same() instead.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static bool ReferenceEquals(object a, object b)
        {
            throw new InvalidOperationException("Assert.ReferenceEquals should not be used");
        }
        #endregion

        public static void EqualDate(DateTime expected, object actual, string message)
        {
            Assert.IsType<DateTime>(actual);
            EqualDate(expected, (DateTime)actual, message);
        }

        public static void EqualDate(DateTime expected, DateTime actual, string message)
        {
            Assert.Equal(expected.Date, actual.Date);
            Assert.True(decimal.Truncate((decimal)expected.TimeOfDay.TotalSeconds) == decimal.Truncate((decimal)actual.TimeOfDay.TotalSeconds), message);
        }

        public static void Equal(SqlExpression expected, SqlExpression actual)
        {
            Assert.True(expected.ToString() == actual.ToString(), "The SQL expressions do not match.");
            Assert.True(expected.Parameters.SequenceEqual(actual.Parameters), "The arguments of the expression do not match.");
        }

        public static void Equal(string expected, SqlExpression actual)
        {
            Assert.Equal(expected, actual.ToString());
        }

        public static void EqualDecoded(string expectedSql, object[] expectedValues, SqlExpression exp)
        {
            using var command = new SqlCommand();
            command.SetExpression(exp);

            Assert.Equal(expectedSql, command.CommandText);
            Assert.Equal(expectedValues.Length, command.Parameters.Count);
            var dbParams = command.Parameters.OfType<DbParameter>().ToArray();

            for (int i = 0; i < dbParams.Length; i++)
                ValidParam(expectedValues[i], command, i);
        }

        public static void ValidParam(object expectedValue, DbCommand cmd, int index)
        {
            var param = cmd.Parameters[index];
            Assert.Equal(string.Concat("@p", index + 1), param.ParameterName);
            Assert.Equal(expectedValue, param.Value);
        }
    }
}

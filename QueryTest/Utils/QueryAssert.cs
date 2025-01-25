using SharpOrm;
using SharpOrm.Builder;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;

namespace QueryTest.Utils
{
    public static class QueryAssert
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

        public static void Equal(Query info, SqlExpression expected, SqlExpression actual, bool allowAlias = false)
        {
            Equal(info.Info, expected, actual, allowAlias);
        }

        public static void Equal(QueryInfo info, SqlExpression expected, SqlExpression actual, bool allowAlias = false)
        {
            actual = new QueryBuilder(info).Add(actual, allowAlias).ToExpression(true);

            Assert.Equal(expected.ToString(), actual.ToString());
            Assert.True(expected.Parameters.SequenceEqual(actual.Parameters), "The arguments of the expression do not match.");
        }

        public static void Equal(SqlExpression expected, SqlExpression actual)
        {
            Assert.Equal(expected.ToString(), actual.ToString());
            Assert.True(expected.Parameters.SequenceEqual(actual.Parameters), "The arguments of the expression do not match.");
        }

        public static void Equal(QueryBase query, string expected, SqlExpression actual)
        {
            Equal(query.Info, expected, actual);
        }

        public static void Equal(QueryBase query, string expected, ISqlExpressible actual)
        {
            Equal(query.Info, expected, actual);
        }

        public static void Equal(IReadonlyQueryInfo info, string expected, ISqlExpressible actual)
        {
            Equal(info, expected, actual.ToExpression(info));
        }

        public static void Equal(IReadonlyQueryInfo info, string expected, SqlExpression actual, bool allowAlias = false)
        {
            Assert.Equal(expected, new QueryBuilder(info).Add(actual, allowAlias).ToExpression(true).ToString());
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
            Assert.True(expectedValues.Length == command.Parameters.Count, $"Expected quantity of SQL parameters {expectedValues.Length}, current {command.Parameters.Count}.");
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

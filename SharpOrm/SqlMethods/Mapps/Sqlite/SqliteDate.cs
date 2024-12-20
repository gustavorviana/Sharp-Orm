using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Sqlite
{
    internal class SqliteDate : SqlPropertyCaller
    {
        private readonly DateOption option;

        public SqliteDate(DateOption option)
        {
            this.option = option;
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            switch (option)
            {
                case DateOption.DateTimeUtc:
                    return new SqlExpression("CURRENT_TIMESTAMP");
                case DateOption.DateOnly:
                    return new SqlExpression("DATE('now')");
                default:
                    return new SqlExpression("datetime()");
            }
        }
    }
}

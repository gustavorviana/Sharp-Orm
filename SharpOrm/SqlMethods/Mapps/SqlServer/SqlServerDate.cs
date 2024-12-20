using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.SqlServer
{
    internal class SqlServerDate : SqlPropertyCaller
    {
        private readonly DateOption option;

        public SqlServerDate(DateOption option)
        {
            this.option = option;
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            switch (option)
            {
                case DateOption.DateTimeUtc:
                    return new SqlExpression("GETUTCDATE()");
                case DateOption.DateOnly:
                    return new SqlExpression("CAST(GETDATE() AS Date)");
                default: 
                    return new SqlExpression("GETDATE()");
            }
        }
    }
}

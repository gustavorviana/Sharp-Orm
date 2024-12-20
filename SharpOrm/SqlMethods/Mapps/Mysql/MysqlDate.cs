using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.SqlMethods.Mapps.Mysql
{
    internal class MysqlDate : SqlPropertyCaller
    {
        private readonly DateOption option;

        public MysqlDate(DateOption option)
        {
            this.option = option;
        }

        protected override SqlExpression GetSqlExpression(IReadonlyQueryInfo info, SqlExpression column, SqlPropertyInfo member)
        {
            switch (option)
            {
                case DateOption.DateTimeUtc:
                    return new SqlExpression("UTC_TIMESTAMP()");
                case DateOption.DateOnly:
                    return new SqlExpression("CURDATE()");
                default:
                    return new SqlExpression("NOW()");
            }
        }
    }
}

using SharpOrm.DataTranslation;
using System.Collections;

namespace SharpOrm.Builder
{
    internal class SafeWhere : ISqlExpressibleAlias
    {
        private readonly string _operation;
        private readonly Column _column;
        private readonly object _value;

        public SafeWhere(string column, string operation, object value)
        {
            _column = new Column(column);
            _operation = operation;
            _value = value;
        }

        public SafeWhere(Column column, string operation, object value)
        {
            _operation = operation;
            _column = column;
            _value = value;
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return ToExpression(info, false);
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            QueryBuilder builder = new QueryBuilder(info);

            if (NeedApplyTableName(info))
                builder.Add(info.Config.ApplyNomenclature(info.TableName.Name)).Add('.');

            var isList = QueryBase.IsCollection(_operation);
            if (isList && _value is ICollection collection && collection.Count == 0)
                return builder.Add("1!=1").ToExpression(info);

            builder
                .AddParameter(_column.ToExpression(info, alias))
                .AddFormat(" {0} ", _operation)
                .AddValue(_value, isList);

            return builder.ToExpression(info);
        }

        private bool NeedApplyTableName(IReadonlyQueryInfo info)
        {
            if (!(info is QueryInfo qInfo) || !(qInfo.Parent is IFkNodeRoot fkRoot))
                return false;

            return fkRoot?.ForeignKeyRegister?.Nodes?.Count > 0 &&
                _column.GetType() == typeof(Column) &&
                !_column.Name.Contains(".");
        }
    }
}

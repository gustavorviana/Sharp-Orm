using System.Text;

namespace SharpOrm.Builder.Expressions
{
    internal class DeferredMemberColumn : SqlExpression, IDeferredSqlExpression
    {
        private readonly IDeferredMemberColumnLoader _loader;
        private readonly int _nodePosition;
        private readonly string _name;

        public DeferredMemberColumn(IDeferredMemberColumnLoader loader)
        {
            Parameters = DotnetUtils.EmptyArray<object>();
            _loader = loader;
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return new SqlExpression(ToString());
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            var node = _loader.GetNode();

            if (_loader.NeedPrefix())
                builder.Append(_loader.GetParentPrefix()).Append('.');

            return builder.Append(_loader.Info.Config.ApplyNomenclature(_loader.ColumnName)).ToString();
        }
    }
}

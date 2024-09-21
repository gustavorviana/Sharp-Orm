using SharpOrm.Builder;
using System;
using System.IO;
using System.Text;

namespace SharpOrm.Fb
{
    internal class FbQueryBuilder : QueryBuilder
    {
        public bool IsBlock { get; set; }

        public FbQueryBuilder(QueryBase query) : base(query)
        {

        }

        protected override SqlExpression InternalGetExpression(string query, object[] @params)
        {
            if (!this.IsBlock || @params.Length == 0)
                return base.InternalGetExpression(query, @params);

            var builder = new StringBuilder();

            builder.Append("EXECUTE BLOCK ");
            builder.Append("(");
            builder.Append(this.GetParamBlockDeclaration(@params[0], 1));

            for (int i = 1; i < @params.Length; i++)
                builder.Append(", ").Append(this.GetParamBlockDeclaration(@params[i], i + 1));

            builder.Append(")");
            builder.Append("AS ");
            builder.Append("BEGIN ");
            builder.Append(query);
            builder.Append("END");

            return new FbSqlExpression(true, builder, @params);
        }

        private string GetParamBlockDeclaration(object value, int index)
        {
            if (value is TimeSpan) return $"p{index} TIME = @p{index}";
            if (value is DateTime || value is DateTimeOffset) return $"p{index} TIMESTAMP = @p{index}";
            if (value is byte[] || value is MemoryStream) return $"p{index} BLOB = @p{index}";

            return $"p{index} CHAR({value?.ToString()?.Length ?? 1}) = @p{index}";
        }
    }
}

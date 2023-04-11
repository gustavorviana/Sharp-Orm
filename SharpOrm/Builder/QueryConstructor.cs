using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryConstructor : IDisposable, IExpressionConversion
    {
        private readonly StringBuilder query = new StringBuilder();
        private readonly List<object> parameters = new List<object>();

        public IReadOnlyCollection<object> Parameters { get; }

        public QueryConstructor()
        {
            this.Parameters = new ReadOnlyCollection<object>(parameters);
        }

        public QueryConstructor Add(SqlExpression exp)
        {
            return this.Add(exp.ToString()).AddParams(exp.Parameters);
        }

        public QueryConstructor Add(string raw = " ")
        {
            this.query.Append(raw);

            return this;
        }

        public QueryConstructor AddParams(params object[] @params)
        {
            foreach (var param in @params)
                this.parameters.Add(param);

            return this;
        }

        public QueryConstructor Line()
        {
            this.query.AppendLine();
            return this;
        }

        public override string ToString()
        {
            return query.ToString();
        }

        ~QueryConstructor()
        {
            ((IDisposable)this).Dispose();
        }

        void IDisposable.Dispose()
        {
            this.query.Clear();
            this.parameters.Clear();
            GC.SuppressFinalize(this);
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return new SqlExpression(this.query.ToString(), this.parameters.ToArray());
        }
    }
}

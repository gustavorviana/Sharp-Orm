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

        public bool Empty => this.query.Length == 0;

        public ReadOnlyCollection<object> Parameters { get; }

        public QueryConstructor()
        {
            this.Parameters = new ReadOnlyCollection<object>(parameters);
        }

        public QueryConstructor Add(QueryConstructor constructor)
        {
            if (constructor == null) 
                throw new ArgumentNullException(nameof(constructor));

            if (this == constructor)
                throw new InvalidOperationException();

            this.Add(constructor.query.ToString());
            this.AddParams(constructor.Parameters);
            return this;
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
            this.parameters.AddRange(@params);

            return this;
        }

        public QueryConstructor AddParams(IEnumerable<object> @params)
        {
            this.parameters.AddRange(@params);

            return this;
        }

        public QueryConstructor Line()
        {
            this.query.AppendLine();
            return this;
        }

        public QueryConstructor Clear()
        {
            this.query.Clear();
            this.parameters.Clear();
            return this;
        }

        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return new SqlExpression(this.query.ToString(), this.parameters.ToArray());
        }

        public override string ToString()
        {
            return query.ToString();
        }

        #region IDisposable
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
        #endregion
    }
}

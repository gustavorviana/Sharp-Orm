using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    public class QueryConstructor : IDisposable, ISqlExpressible
    {
        private readonly StringBuilder query = new StringBuilder();
        private readonly List<object> parameters = new List<object>();

        public bool Empty => this.query.Length == 0;

        public ReadOnlyCollection<object> Parameters { get; }

        public QueryConstructor()
        {
            this.Parameters = new ReadOnlyCollection<object>(parameters);
        }

        public QueryConstructor SafeAddParam(IReadonlyQueryInfo info, object val, bool addParamChar = true, bool allowAlias = true)
        {
            if (val is SqlExpression exp)
                return this.Add(exp, info, allowAlias);

            if (val is ISqlExpressible iExp)
                return AddExpression(info, iExp, allowAlias);

            if (addParamChar)
                this.Add("?");

            return this.AddParams(val);
        }

        public QueryConstructor AddExpression(IReadonlyQueryInfo info, ISqlExpressible exp, bool allowAlias = true)
        {
            return this.Add(exp.ToSafeExpression(info, allowAlias), info, allowAlias);
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

        public QueryConstructor Add(SqlExpression exp, IReadonlyQueryInfo info, bool allowAlias)
        {
            StringBuilder builder = new StringBuilder(exp.ToString());
            int[] paramCharIndexes = builder.GetIndexesOfParamsChar().ToArray();

            for (int i = paramCharIndexes.Length - 1; i >= 0; i--)
                if (exp.Parameters[i] is ISqlExpressible iExp)
                    this.AddParams(this.SafeAdd(paramCharIndexes[i], info, builder, iExp, allowAlias));
                else
                    this.SafeAddParam(info, exp.Parameters[i], false);

            this.Add(builder.ToString());
            return this;
        }

        private IEnumerable<object> SafeAdd(int argIndex, IReadonlyQueryInfo info, StringBuilder builder, ISqlExpressible exp, bool allowAlias)
        {
            builder.Remove(argIndex, 1);
            var sqlExp = exp.ToSafeExpression(info, allowAlias);

            builder.Insert(argIndex, sqlExp.ToString());
            foreach (var item in sqlExp.Parameters)
                yield return item;
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// A class for building SQL queries with parameters.
    /// </summary>
    public class QueryConstructor : IDisposable, ISqlExpressible
    {
        private readonly StringBuilder query = new StringBuilder();
        private readonly List<object> parameters = new List<object>();
        private readonly IReadonlyQueryInfo info;

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        public bool Empty => this.query.Length == 0;

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public ReadOnlyCollection<object> Parameters { get; }

        public QueryConstructor(QueryInfo info) : this(info.ToReadOnly())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryConstructor"/> class.
        /// </summary>
        /// <param name="info">The query information.</param>
        public QueryConstructor(IReadonlyQueryInfo info)
        {
            this.info = info;
            this.Parameters = new ReadOnlyCollection<object>(parameters);
        }

        public QueryConstructor AddParameter(object val, bool allowAlias = true)
        {
            return this.InternalAddParameter(val, true, allowAlias);
        }

        /// <summary>
        /// Adds an expression to the query.
        /// </summary>
        /// <param name="expression">The expression to add.</param>
        /// <param name="allowAlias">Whether to allow aliases in the parameter name.</param>
        public QueryConstructor AddExpression(ISqlExpressible expression, bool allowAlias = true)
        {
            return this.Add(expression.ToSafeExpression(this.info, allowAlias), allowAlias);
        }

        /// <summary>
        /// Adds a <see cref="QueryConstructor"/> to this instance.
        /// </summary>
        /// <param name="constructor">The <see cref="QueryConstructor"/> to add.</param>
        public QueryConstructor Add(QueryConstructor constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));

            if (this == constructor)
                throw new InvalidOperationException();

            this.Add(constructor.query.ToString());
            this.AddParameters(constructor.Parameters);
            return this;
        }

        /// <summary>
        /// Adds an <see cref="SqlExpression"/> to the query.
        /// </summary>
        /// <param name="expression">The <see cref="SqlExpression"/> to add.</param>
        public QueryConstructor Add(SqlExpression expression)
        {
            return this.Add(expression.ToString()).AddParameters(expression.Parameters);
        }

        /// <summary>
        /// Adds an <see cref="SqlExpression"/> to the query.
        /// </summary>
        /// <param name="expression">The <see cref="SqlExpression"/> to add
        /// <param name="allowAlias">Whether to allow aliases in the parameter name.</param>
        public QueryConstructor Add(SqlExpression exp, bool allowAlias)
        {
            StringBuilder builder = new StringBuilder(exp.ToString());
            int[] paramCharIndexes = builder.GetIndexesOfParamsChar().ToArray();

            for (int i = paramCharIndexes.Length - 1; i >= 0; i--)
                if (exp.Parameters[i] is ISqlExpressible iExp)
                    this.AddParameters(this.InternalAdd(paramCharIndexes[i], builder, iExp, allowAlias));
                else
                    this.InternalAddParameter(exp.Parameters[i], false, true);

            this.Add(builder.ToString());
            return this;
        }

        private QueryConstructor InternalAddParameter(object val, bool addParamChar, bool allowAlias)
        {
            if (val is SqlExpression exp)
                return this.Add(exp, allowAlias);

            if (val is ISqlExpressible iExp)
                return this.AddExpression(iExp, allowAlias);

            if (addParamChar)
                this.Add("?");

            return this.AddParameters(val);
        }

        private IEnumerable<object> InternalAdd(int argIndex, StringBuilder builder, ISqlExpressible exp, bool allowAlias)
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

        /// <summary>
        /// Adds parameters to the query.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        public QueryConstructor AddParameters(params object[] parameters)
        {
            return AddParameters((IEnumerable<object>)parameters);
        }

        /// <summary>
        /// Adds parameters to the query.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        public QueryConstructor AddParameters(IEnumerable<object> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            this.parameters.AddRange(parameters);

            return this;
        }

        /// <summary>
        /// Adds a line break to the query.
        /// </summary>
        public QueryConstructor NewLine()
        {
            this.query.AppendLine();
            return this;
        }

        /// <summary>
        /// Clears the query.
        /// </summary>
        public QueryConstructor Clear()
        {
            this.query.Clear();
            this.parameters.Clear();
            return this;
        }

        /// <summary>
        /// Returns an <see cref="SqlExpression"/> representation of the query.
        /// </summary>
        /// <param name="info">The query information.</param>
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

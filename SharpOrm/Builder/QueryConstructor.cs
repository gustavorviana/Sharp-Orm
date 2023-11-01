using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// A class for building SQL queries with parameters.
    /// </summary>
    public sealed class QueryConstructor : IDisposable, ISqlExpressible
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
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

        public QueryConstructor(QueryBase query) : this(query.Info)
        {

        }

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

        /// <summary>
        /// Adds a <see cref="QueryConstructor"/> to this instance.
        /// </summary>
        /// <param name="constructor">The <see cref="QueryConstructor"/> to add.</param>
        public QueryConstructor Add(QueryConstructor constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));

            if (this.Equals(constructor))
                throw new InvalidOperationException("The same instance cannot be passed as a parameter.");

            return this.Add(constructor.query.ToString()).AddParameters(constructor.Parameters);
        }

        /// <summary>
        /// Adds an <see cref="SqlExpression"/> to the query.
        /// </summary>
        /// <param name="expression">The <see cref="SqlExpression"/> to add.</param>
        public QueryConstructor Add(SqlExpression expression)
        {
            return this.Add(expression, false);
        }

        /// <summary>
        /// Adds an <see cref="SqlExpression"/> to the query.
        /// </summary>
        /// <param name="expression">The <see cref="SqlExpression"/> to add
        /// <param name="allowAlias">Whether to allow aliases in the parameter name.</param>
        public QueryConstructor Add(SqlExpression expression, bool allowAlias)
        {
            return this.Add(expression.ToString(), allowAlias, expression.Parameters);
        }

        public QueryConstructor Add(string query, params object[] parameters)
        {
            return this.Add(query, false, parameters);
        }

        public QueryConstructor Add(string query, bool allowAlias, params object[] parameters)
        {
            if (query.Count(c => c == '?') != parameters.Length)
                throw new InvalidOperationException("The operation cannot be performed because the arguments passed in the SQL query do not match the provided parameters.");

            this.query.AppendReplaced(query, '?', count =>
            {
                this.AddParameter(parameters[count - 1], true);
                return null;
            });

            return this;
        }

        public QueryConstructor AddParameter(object val, bool allowAlias = true)
        {
            if (ToQueryValue(val) is string sql)
                return this.Add(sql);

            if (val is SqlExpression exp)
                return this.Add(exp, allowAlias);

            if (val is ISqlExpressible iExp)
                return this.AddExpression(iExp, allowAlias);

            return this.Add("?").AddParameters(val);
        }

        public QueryConstructor Add(object rawQuery)
        {
            if (ToQueryValue(rawQuery) is string strQuery)
                return this.Add(strQuery);

            return this.Add(rawQuery?.ToString());
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

        public static string ToQueryValue(object value)
        {
            if (TranslationUtils.IsNull(value))
                return "NULL";

            if (value is bool vBool)
                return vBool ? "1" : "0";

            if (TranslationUtils.IsNumeric(value?.GetType()))
                return ((IConvertible)value).ToString(Invariant);

            return null;
        }

        public QueryConstructor AddFormat(string format, params object[] args)
        {
            this.query.AppendFormat(format, args);
            return this;
        }

        public QueryConstructor Add(char raw = ' ')
        {
            this.query.Append(raw);
            return this;
        }

        public QueryConstructor Add(string raw)
        {
            this.query.Append(raw);
            return this;
        }

        public QueryConstructor Add(params string[] raws)
        {
            this.query.Capacity += raws.Sum(txt => txt.Length) + raws.Length;
            return this.AddJoin(" ", raws);
        }

        public QueryConstructor AddJoin<T>(string separator, IEnumerable<T> values)
        {
            this.query.AppendJoin(separator, values);
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

        public QueryConstructor WriteEnumerableAsValue(IEnumerable enumerable, bool allowAlias)
        {
            var @enum = enumerable.GetEnumerator();
            if (!@enum.MoveNext())
                throw new InvalidOperationException(Messages.CannotUseEmptyCollection);

            this.Add('(');
            this.AddParameter(@enum.Current, allowAlias);

            while (@enum.MoveNext())
                this.Add(", ").AddParameter(@enum.Current, allowAlias);

            return this.Add(')');
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

        internal void FixLastOpen()
        {
            if (this.query[this.query.Length - 1] == '(') this.query.Remove(this.query.Length - 1, 1);
            else this.query.Append(')');
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

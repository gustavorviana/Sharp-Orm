using SharpOrm.DataTranslation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// A class for building SQL queries with parameters.
    /// </summary>
    public class QueryBuilder : IDisposable, ISqlExpressible
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        internal readonly StringBuilder query = new StringBuilder();
        /// <summary>
        /// A list of parameters for the SQL query.
        /// </summary>
        protected readonly List<object> parameters = new List<object>();
        private readonly IReadonlyQueryInfo info;

        internal Func<object, object> paramInterceptor;

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        public bool Empty => this.query.Length == 0;

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public ReadOnlyCollection<object> Parameters { get; }

        /// <summary>
        /// Create an instance of <see cref="QueryBuilder"/> using the query configuration from a <see cref="QueryBase"/>.
        /// </summary>
        /// <param name="query"></param>
        public QueryBuilder(QueryBase query) : this(query.Info)
        {

        }

        /// <summary>
        /// Create an instance of <see cref="QueryBuilder"/> using a <see cref="QueryConfig"/> and a <see cref="DbName"/>.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="table"></param>
        public QueryBuilder(QueryConfig config, DbName table) : this(new ReadonlyQueryInfo(config, table))
        {

        }

        /// <summary>
        /// Create an instance of <see cref="QueryBuilder"/> using the query configuration from a <see cref="QueryInfo"/>.
        /// </summary>
        /// <param name="info"></param>
        public QueryBuilder(QueryBaseInfo info) : this(info.ToReadOnly())
        {
        }

        /// <summary>
        /// Create an instance of <see cref="QueryBuilder"/> using <see cref="IReadonlyQueryInfo"/>.
        /// </summary>
        /// <param name="info">The query information.</param>
        public QueryBuilder(IReadonlyQueryInfo info)
        {
            this.info = info;
            this.Parameters = new ReadOnlyCollection<object>(parameters);
        }

        /// <summary>
        /// Adds a <see cref="QueryBuilder"/> to this instance.
        /// </summary>
        /// <param name="builder">The <see cref="QueryBuilder"/> to add.</param>
        public QueryBuilder Add(QueryBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (this.Equals(builder))
                throw new InvalidOperationException("The same instance cannot be passed as a parameter.");

            return this.Add(builder.query.ToString()).AddParameters(builder.Parameters);
        }

        /// <summary>
        /// Adds an <see cref="SqlExpression"/> to the query.
        /// </summary>
        /// <param name="expression">The <see cref="SqlExpression"/> to add.</param>
        public QueryBuilder Add(SqlExpression expression)
        {
            return this.Add(expression, false);
        }

        /// <summary>
        /// Adds an <see cref="SqlExpression"/> to the query.
        /// </summary>
        /// <param name="expression">The <see cref="SqlExpression"/> to add
        /// <param name="allowAlias">Whether to allow aliases in the parameter name.</param>
        public QueryBuilder Add(SqlExpression expression, bool allowAlias)
        {
            return this.Add(expression.ToString(), allowAlias, expression.Parameters);
        }

        /// <summary>
        /// Adds a SQL query with parameters to the instance.
        /// </summary>
        /// <param name="query">The SQL query to be added.</param>
        /// <param name="parameters">The parameters to be replaced in the query.</param>
        public QueryBuilder Add(string query, params object[] parameters)
        {
            return this.Add(query, false, parameters);
        }

        /// <summary>
        /// Adds a SQL query with parameters to the instance.
        /// </summary>
        /// <param name="query">The SQL query to be added.</param>
        /// <param name="allowAlias">Indicates whether aliases are allowed in the parameter name.</param>
        /// <param name="parameters">The parameters to be replaced in the query.</param>
        /// <exception cref="InvalidOperationException">Thrown when the number of parameters in the query does not match the number of provided parameters.</exception>
        public QueryBuilder Add(string query, bool allowAlias, params object[] parameters)
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

        /// <summary>
        /// Adds a SQL query with parameters to the instance.
        /// </summary>
        /// <param name="query">The SQL query to be added.</param>
        /// <param name="allowAlias">Indicates whether aliases are allowed in the parameter name.</param>
        /// <param name="parameters">The parameters to be replaced in the query.</param>
        /// <exception cref="InvalidOperationException">Thrown when the number of parameters in the query does not match the number of provided parameters.</exception>
        public QueryBuilder AddAndReplace(string query, char toReplace, Func<int, string> func)
        {
            this.query.AppendReplaced(query, toReplace, func);
            return this;
        }

        /// <summary>
        /// Adds a SQL query with parameters to the instance.
        /// </summary>
        /// <param name="query">The SQL query to be added.</param>
        /// <param name="allowAlias">Indicates whether aliases are allowed in the parameter name.</param>
        /// <param name="parameters">The parameters to be replaced in the query.</param>
        /// <exception cref="InvalidOperationException">Thrown when the number of parameters in the query does not match the number of provided parameters.</exception>
        public QueryBuilder AddAndReplace(string query, char toReplace, Action<int> call)
        {
            this.query.AppendAndReplace(query, toReplace, call);
            return this;
        }

        /// <summary>
        /// Adds a parameter to the SQL query.
        /// </summary>
        /// <param name="val">The value of the parameter to be added.</param>
        /// <param name="allowAlias">Indicates whether aliases are allowed in the parameter name.</param>
        public QueryBuilder AddParameter(object val, bool allowAlias = true)
        {
            if (val is SqlExpression exp)
                return this.Add(exp, allowAlias);

            if (val is ISqlExpressible iExp)
                return this.AddExpression(iExp, allowAlias);

            val = (this.info?.Config?.Translation ?? TranslationRegistry.Default).ToSql(val);
            if (paramInterceptor != null)
                val = paramInterceptor(val);

            if (ToQueryValue(val) is string sql)
                return this.Add(sql);

            if (this.info?.Config?.EscapeStrings == true && val is string strVal)
                return this.Add(this.info.Config.EscapeString(strVal));

            if (!(val is byte[]) && val is ICollection)
                throw new NotSupportedException();

            if (val is System.IO.MemoryStream ms)
                return this.InternalAddParam(ms.ToArray());

            return this.InternalAddParam(val);
        }

        /// <summary>
        /// Adds a parameter to the SQL query and returns the updated <see cref="QueryBuilder"/>.
        /// </summary>
        /// <param name="value">The value to be added as a parameter.</param>
        /// <returns>The updated <see cref="QueryBuilder"/> with the parameter added.</returns>
        protected virtual QueryBuilder InternalAddParam(object value)
        {
            this.parameters.Add(value);
            return this.Add("?");
        }

        /// <summary>
        /// Adds a SQL query or raw value to the instance.
        /// </summary>
        /// <param name="rawQuery">The SQL query or raw value to be added.</param>
        public QueryBuilder Add(object rawQuery)
        {
            if (ToQueryValue(rawQuery) is string strQuery)
                return this.Add(strQuery);

            return this.Add(rawQuery?.ToString());
        }

        /// <summary>
        /// Adds an values to the query.
        /// </summary>
        /// <param name="expression">The values to add.</param>
        /// <param name="allowAlias">Whether to allow aliases in the parameter name.</param>
        public QueryBuilder AddExpression(ISqlExpressible expression, bool allowAlias = true)
        {
            return this.Add(expression.ToSafeExpression(this.info, allowAlias), allowAlias);
        }

        /// <summary>
        /// Converts an object to its SQL query representation.
        /// </summary>
        /// <param name="value">The object to be converted to a SQL query value.</param>
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

        /// <summary>
        /// Adds formatted text to the query using the specified format string and arguments.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An array of objects to be formatted and inserted into the string.</param>
        /// <returns>The current instance of QueryBuilder with the formatted text added.</returns>
        public QueryBuilder AddFormat(string format, params object[] args)
        {
            this.query.AppendFormat(format, args);
            return this;
        }

        /// <summary>
        /// Adds a character or raw text to the query.
        /// </summary>
        /// <param name="raw">The character or raw text to be added to the query.</param>
        /// <returns>The current instance of QueryBuilder with the character or raw text added.</returns>
        public QueryBuilder Add(char raw = ' ')
        {
            this.query.Append(raw);
            return this;
        }

        /// <summary>
        /// Adds raw text to the query.
        /// </summary>
        /// <param name="raw">The raw text to be added to the query.</param>
        public QueryBuilder Add(string raw)
        {
            this.query.Append(raw);
            return this;
        }

        /// <summary>
        /// Adds multiple raw strings to the query, joining them with a space separator.
        /// </summary>
        /// <param name="raws">An array of raw strings to be added to the query.</param>
        public QueryBuilder AddRaws(params string[] raws)
        {
            this.query.Capacity += raws.Sum(txt => txt.Length) + raws.Length;
            return this.AddJoin(" ", raws);
        }

        /// <summary>
        /// Adds multiple values to the query, joining them with a specified separator.
        /// </summary>
        /// <typeparam name="T">The type of values to be joined.</typeparam>
        /// <param name="separator">The separator used to join the values.</param>
        /// <param name="values">An enumerable collection of values to be joined and added to the query.</param>
        public QueryBuilder AddJoin<T>(string separator, IEnumerable<T> values)
        {
            BuilderExt.AppendJoin(this.query, separator, values);
            return this;
        }


        /// <summary>
        /// Adds multiple values to the query, joining them with a specified separator.
        /// </summary>
        public QueryBuilder AddJoin<T>(Action<T> callback, string separator, IEnumerator<T> en)
        {
            this.query.AppendJoin(callback, separator, en);
            return this;
        }

        /// <summary>
        /// Adds parameters to the query.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        public QueryBuilder AddParameters(params object[] parameters)
        {
            return AddParameters((IEnumerable<object>)parameters);
        }

        /// <summary>
        /// Adds parameters to the query.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        public QueryBuilder AddParameters(IEnumerable<object> parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            this.parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        /// Converts an enumerable collection to a SQL value list and adds it to the query.
        /// </summary>
        /// <param name="enumerable">The enumerable collection to be converted and added as a SQL value list.</param>
        /// <param name="allowAlias">Indicates whether aliases are allowed in the parameter name.</param>
        /// <exception cref="InvalidOperationException">Thrown when the enumerable collection is empty.</exception>
        public QueryBuilder WriteEnumerableAsValue(IEnumerable enumerable, bool allowAlias)
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
        public QueryBuilder NewLine()
        {
            this.query.AppendLine();
            return this;
        }

        /// <summary>
        /// Clears the query.
        /// </summary>
        public virtual QueryBuilder Clear()
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
            return this.ToExpression();
        }

        /// <summary>
        /// Returns an <see cref="SqlExpression"/> representation of the query.
        /// </summary>
        /// <param name="info">The query information.</param>
        public SqlExpression ToExpression()
        {
            return new SqlExpression(this.query.ToString(), this.parameters.ToArray());
        }

        /// <summary>
        /// Returns the SQL query as a string representation.
        /// </summary>
        /// <returns>The SQL query as a string.</returns>
        public override string ToString()
        {
            return query.ToString();
        }

        #region IDisposable
        ~QueryBuilder()
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

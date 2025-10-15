using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// Represents a column in a database table.
    /// </summary>
    public class Column : ISqlExpressibleAlias, IEquatable<Column>, IEquatable<string>
    {
        #region Fields\Properties
        protected readonly SqlExpression _expression;
        private readonly ISqlExpressible _expressible;
        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the collation of the column.
        /// </summary>
        public string Collate { get; set; }

        /// <summary>
        /// Gets or sets the alias of the column.
        /// </summary>
        public string Alias { get; set; }

        private bool? isCount;
        /// <summary>
        /// Gets a value indicating whether the column represents a count expression.
        /// </summary>
        public bool IsCount
        {
            get
            {
                if (isCount == null)
                    isCount = _expression?.ToString() is string exp && exp.StartsWith("count(", StringComparison.OrdinalIgnoreCase) && exp.EndsWith(")");

                return isCount.Value;
            }
        }

        /// <summary>
        /// Gets a column representing all columns with the wildcard (*).
        /// </summary>
        public static readonly Column All = (Column)"*";
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class.
        /// </summary>
        protected Column()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class by copying the specified column.
        /// </summary>
        /// <param name="column">The column to copy.</param>
        protected Column(Column column)
        {
            _expression = column._expression;
            Name = column.Name;
            Alias = column.Alias;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class with the specified name and alias.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="alias">The alias of the column.</param>
        public Column(string name, string alias)
        {
            Name = name;
            Alias = alias;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class with the specified full name.
        /// </summary>
        /// <param name="fullName">The full name of the column.</param>
        public Column(string fullName) : this(new DbName(fullName))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class with the specified <see cref="DbName"/>.
        /// </summary>
        /// <param name="name">The <see cref="DbName"/> representing the column.</param>
        public Column(DbName name)
        {
            Name = name.Name;
            Alias = name.Alias;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class with the specified SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression representing the column.</param>
        public Column(SqlExpression expression)
        {
            _expression = expression;
        }

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="Column"/> com o objeto <see cref="ISqlExpressible"/> especificado.
        /// </summary>
        /// <param name="expression">Expressão SQL que representa a coluna.</param>
        public Column(ISqlExpressible expression)
        {
            _expressible = expression;
        }

        /// <summary>
        /// Tries to parse the specified name into a Column object.
        /// </summary>
        /// <param name="name">The name to parse.</param>
        /// <param name="column">When this method returns, contains <see cref="Column"/> if the parse operation succeeded, or null if the parse failed.</param>
        /// <returns>true if the name was successfully parsed; otherwise, false.</returns>
        public static bool TryParse(string name, out Column column)
        {
            if (string.IsNullOrEmpty(name) || !DbName.IsValidName(name))
            {
                column = null;
                return false;
            }

            column = new Column(name, "");
            return true;
        }

        /// <summary>
        /// Tries to parse the specified name and alias into a Column object.
        /// </summary>
        /// <param name="name">The name to parse.</param>
        /// <param name="alias">The alias to parse.</param>
        /// <param name="column">When this method returns, contains <see cref="Column"/> if the parse operation succeeded, or null if the parse failed.</param>
        /// <returns>true if the name and alias were successfully parsed; otherwise, false.</returns>
        public static bool TryParse(string name, string alias, out Column column)
        {
            if (string.IsNullOrEmpty(name) || !DbName.IsValidName(name) || string.IsNullOrEmpty(alias) || !DbName.IsValidAlias(alias))
            {
                column = null;
                return false;
            }

            column = new Column(name, alias);
            return true;
        }

        /// <summary>
        /// Converts the column to a <see cref="SqlExpression"/> based on the provided query information.
        /// </summary>
        /// <param name="info">The query information.</param>
        /// <returns>A <see cref="SqlExpression"/> representing the column.</returns>
        public SqlExpression ToExpression(IReadonlyQueryInfo info)
        {
            return this.ToExpression(info, true);
        }

        /// <summary>
        /// Converts the column to a <see cref="SqlExpression"/> based on the provided query information.
        /// </summary>
        /// <param name="info">The query information.</param>
        /// <param name="alias">A flag indicating whether to include the alias in the expression.</param>
        /// <returns>A <see cref="SqlExpression"/> representing the column.</returns>
        public virtual SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            if (_expression != null)
                return _expression;

            if (_expressible != null)
                return _expressible.ToSafeExpression(info, alias);

            StringBuilder builder = new StringBuilder();
            builder.Append(GetName(info));

            if (UseCollate())
                builder.Append(" COLLATE ").Append(Collate);

            if (alias && !string.IsNullOrEmpty(Alias))
                builder.Append(" AS ").Append(info.Config.ApplyNomenclature(Alias));

            return (SqlExpression)builder;
        }

        protected virtual string GetName(IReadonlyQueryInfo info)
        {
            return info.Config.ApplyNomenclature(Name);
        }

        private bool UseCollate()
        {
            if (string.IsNullOrEmpty(Collate))
                return false;

            if (!DbName.IsValid(Collate))
                throw new InvalidCollateNameException(Collate);

            return true;
        }

        public static explicit operator Column(string rawColumn)
        {
            return new Column(new SqlExpression(rawColumn));
        }

        /// <summary>
        /// Retrieves a column that represents the last field or property of the expression (manually mapped objects not included).
        /// </summary>
        /// <param name="columnExpression"></param>
        /// <returns></returns>
        [Obsolete("This method will be removed in version 4.x")]
        public static Column FromExp<T>(Expression<ColumnExpression<T>> columnExpression)
        {
            return ExpressionUtils<T>.GetColumn(columnExpression);
        }

        /// <summary>
        /// Retrieves a column that represents the last field or property of the expression, including manually mapped objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnExpression"></param>
        /// <param name="registry"></param>
        /// <returns></returns>
        [Obsolete("This method will be removed in version 4.x")]
        public static Column FromExp<T>(Expression<ColumnExpression<T>> columnExpression, TranslationRegistry registry)
        {
            var member = ExpressionUtils<T>.GetColumnMember(columnExpression, out var rootType);
            if (!(registry.GetTable(rootType) is TableInfo table))
                return new MemberInfoColumn(member);

            string name = table.GetColumn(member)?.Name;
            if (string.IsNullOrEmpty(name))
                return null;

            return new MemberInfoColumn(member, name);
        }

        /// <summary>
        /// Parses a column expression and returns a Column object.
        /// </summary>
        /// <typeparam name="T">The type of the object containing the column.</typeparam>
        /// <param name="expression">The column expression to parse.</param>
        /// <param name="info">The read-only query information.</param>
        /// <returns>A Column object representing the parsed column expression.</returns>
        [Obsolete("This method will be removed in version 4.x")]
        public static Column Parse<T>(Expression<ColumnExpression<T>> expression, IReadonlyQueryInfo info)
        {
            return new ExpressionProcessor<T>(info, info?.Config?.Translation, ExpressionConfig.All, null).ParseColumns(expression).First();
        }

        /// <summary>
        /// Parses a column expression and returns a Column object.
        /// </summary>
        /// <typeparam name="T">The type of the object containing the column.</typeparam>
        /// <typeparam name="R">The type of the column value.</typeparam>
        /// <param name="expression">The column expression to parse.</param>
        /// <param name="info">The read-only query information.</param>
        /// <returns>A Column object representing the parsed column expression.</returns>
        [Obsolete("This method will be removed in version 4.x")]
        public static Column Parse<T, R>(Expression<ColumnExpression<T, R>> expression, IReadonlyQueryInfo info)
        {
            return new ExpressionProcessor<T>(info, info?.Config?.Translation, ExpressionConfig.All, null).ParseColumn(expression);
        }

        internal string GetCountColumn()
        {
            if (IsAll())
                return "*";

            string exp = _expression?.ToString();
            return IsCount ? exp.Substring(6, exp.Length - 8) : Name;
        }

        internal bool IsAll()
        {
            return (_expression?.ToString() ?? Name).EndsWith("*");
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("Column(");

            if (_expression != null)
                return builder.Append(_expressible.ToString()).Append(')').ToString();

            if (_expression != null) builder.Append(_expression.ToString());
            else builder.Append(Name.Trim());

            if (UseCollate())
                builder.Append(" COLLATE ").Append(Collate);

            if (_expression == null && !string.IsNullOrEmpty(Alias))
                builder.AppendFormat(" AS {0}", Alias.Trim());

            return builder.Append(')').ToString();
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as Column);
        }

        public bool Equals(Column other)
        {
            return other != null &&
                   Name == other.Name &&
                   Alias == other.Alias &&
                   Expression.Equals(_expression, other._expression);
        }

        public override int GetHashCode()
        {
            return -1584136870 + EqualityComparer<string>.Default.GetHashCode(Name);
        }

        public bool Equals(string other)
        {
            return other != null &&
                   Name == other;
        }

        public static bool operator ==(Column left, Column right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Column left, Column right)
        {
            return !(left == right);
        }

        #endregion
    }
}

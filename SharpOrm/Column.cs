﻿using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
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
        protected readonly SqlExpression expression;
        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the alias of the column.
        /// </summary>
        public string Alias { get; set; }

        private bool? isCount;
        public bool IsCount
        {
            get
            {
                if (this.isCount == null)
                    this.isCount = this.expression?.ToString() is string exp && exp.StartsWith("count(", StringComparison.OrdinalIgnoreCase) && exp.EndsWith(")");

                return this.isCount.Value;
            }
        }

        /// <summary>
        /// Gets a column representing all columns with the wildcard (*).
        /// </summary>
        public static Column All => (Column)"*";
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
            this.expression = column.expression;
            this.Name = column.Name;
            this.Alias = column.Alias;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class with the specified name and alias.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="alias">The alias of the column.</param>
        public Column(string name, string alias)
        {
            this.Name = name;
            this.Alias = alias;
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
            this.Name = name.Name;
            this.Alias = name.Alias;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Column"/> class with the specified SQL expression.
        /// </summary>
        /// <param name="expression">The SQL expression representing the column.</param>
        public Column(SqlExpression expression)
        {
            this.expression = expression;
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
            if (this.expression != null)
                return this.expression;

            StringBuilder builder = new StringBuilder();
            builder.Append(info.Config.ApplyNomenclature(this.Name));

            if (alias && !string.IsNullOrEmpty(this.Alias))
                builder.Append(" AS ").Append(info.Config.ApplyNomenclature(this.Alias));

            return (SqlExpression)builder;
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
        public static Column FromExp<T>(Expression<ColumnExpression<T>> columnExpression, TranslationRegistry registry)
        {
            var member = ExpressionUtils<T>.GetColumnMember(columnExpression, out var rootType);
            if (registry.GetTable(rootType) is TableInfo table)
                return new MemberInfoColumn(member, table.GetColumn(member).Name);

            return new MemberInfoColumn(member);
        }

        internal string GetCountColumn()
        {
            if (this.IsAll())
                return "*";

            string exp = this.expression?.ToString();
            return this.IsCount ? exp.Substring(6, exp.Length - 2) : this.Name;
        }

        internal bool IsAll()
        {
            return (this.expression?.ToString() ?? this.Name).EndsWith("*");
        }

        #region IEquatable

        public override bool Equals(object obj)
        {
            return Equals(obj as Column);
        }

        public bool Equals(Column other)
        {
            return other is Column &&
                   Name == other.Name &&
                   Alias == other.Alias &&
                   expression.Equals(other.expression);
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

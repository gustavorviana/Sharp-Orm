using SharpOrm.Builder.Grammars;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using SharpOrm.SqlMethods;
using System;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Class responsible for loading query management information, its configurations, and translations. Doc: https://github.com/gustavorviana/Sharp-Orm/wiki/QueryConfig
    /// </summary>
    public abstract class QueryConfig : ICloneable
    {
        /// <summary>
        /// Gets the maximum number of parameters allowed in a database query.
        /// </summary>
        public virtual int DbParamsLimit { get; } = 2099;

        /// <summary>
        /// Indicates whether the DBMS supports native upsert rows.
        /// </summary>
        internal protected virtual bool NativeUpsertRows { get; }

        /// <summary>
        /// If the model has one or more validations defined, they will be checked before saving or updating.
        /// </summary>
        public bool ValidateModelOnSave { get; set; }

        /// <summary>
        /// Indicate whether the DBMS supports performing an update or delete with the SQL join clause.
        /// </summary>
        public virtual bool CanUpdateJoin { get; } = true;

        /// <summary>
        /// Instance that will be responsible for managing translations.
        /// </summary>
        public TranslationRegistry Translation { get; set; } = TranslationRegistry.Default;

        /// <summary>
        /// Custom mapping of columns in the database.
        /// </summary>
        public ColumnTypeMap[] CustomColumnTypes { get; set; } = DotnetUtils.EmptyArray<ColumnTypeMap>();

        /// <summary>
        /// Registry of SQL methods.
        /// </summary>
        public virtual SqlMethodRegistry Methods { get; } = new SqlMethodRegistry();

        /// <summary>
        /// Indicates if value modifications in the table should be made with "WHERE" (this is not valid for insert-and-select).
        /// </summary>
        public bool OnlySafeModifications { get; }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// If enabled, allows the query to create an object only with its primary key when there is no depth and allows reading the id of a foreign object on insert or update.
        /// </summary>
        public bool LoadForeign { get; set; }

        /// <summary>
        /// Indicates whether the generated key from the SQL insert operation 
        /// should be applied to the object that was inserted into the table.
        /// (only for objects with a primary key).
        /// </summary>
        public bool ApplyGeneratedKey { get; set; } = true;

        /// <summary>
        /// If true, parameters will be used; if false, strings will be manually escaped.
        /// </summary>
        /// <remarks>
        /// Use this option with caution, as it can cause issues in the execution of your scripts.
        /// </remarks>
        public bool EscapeStrings { get; set; }

        /// <summary>
        /// Gets or sets the mode for mapping nested objects.
        /// </summary>
        public NestedMode NestedMapMode { get; set; } = NestedMode.Attribute;

        /// <summary>
        /// Create an instance that allows only safe modifications.
        /// </summary>
        public QueryConfig() : this(true)
        {
            RegisterMethods();
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="safeModificationsOnly">Signal whether only safe modifications should be made.</param>
        /// <remarks>Safe modifications are updates and deletes with a WHERE clause.</remarks>
        public QueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
            RegisterMethods();
        }

        /// <summary>
        /// Create a new instance with specified methods.
        /// </summary>
        /// <param name="safeModificationsOnly">Signal whether only safe modifications should be made.</param>
        /// <param name="methods">The SQL method registry.</param>
        protected QueryConfig(bool safeModificationsOnly, SqlMethodRegistry methods)
        {
            this.OnlySafeModifications = safeModificationsOnly;
            this.Methods = methods;
        }

        /// <summary>
        /// Registers SQL methods.
        /// </summary>
        protected virtual void RegisterMethods()
        {

        }

        /// <summary>
        /// Fix table name, column and alias for SQL.
        /// </summary>
        /// <param name="name">The name to apply nomenclature to.</param>
        /// <returns>The fixed name.</returns>
        public virtual string ApplyNomenclature(string name) => name;

        /// <summary>
        /// Creates a new grammar object.
        /// </summary>
        /// <param name="query">Query for grammar.</param>
        /// <returns>A new grammar object.</returns>
        public abstract Grammar NewGrammar(Query query);

        /// <summary>
        /// Creates a new instance of <see cref="TableGrammar"/> for the specified schema.
        /// </summary>
        /// <param name="schema">The table schema.</param>
        /// <returns>A new instance of <see cref="TableGrammar"/>.</returns>
        /// <exception cref="NotSupportedException">Thrown when the derived class does not support creating/editing/removing tables.</exception>
        public virtual TableGrammar NewTableGrammar(TableSchema schema)
        {
            throw new NotSupportedException(string.Format(Messages.TableManagementNotSupported, GetType().FullName));
        }

        /// <summary>
        /// Escapes a string for use in a SQL query.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public abstract string EscapeString(string value);

        object ICloneable.Clone() => this.Clone();

        /// <summary>
        /// Creates a copy of the current <see cref="QueryConfig"/> instance.
        /// </summary>
        /// <returns>A new instance of <see cref="QueryConfig"/> that is a copy of the current instance.</returns>
        public abstract QueryConfig Clone(bool? safeOperations = null);

        /// <summary>
        /// Copies the properties of the current <see cref="QueryConfig"/> instance to the target instance.
        /// </summary>
        /// <param name="target">The target instance to copy the properties to.</param>
        protected void CopyTo(QueryConfig target)
        {
            foreach (var prop in this.GetType().GetProperties(Bindings.Instance))
                if (prop.CanRead && prop.CanWrite)
                    ReflectionUtils.CopyPropTo(this, target, prop);
        }

        /// <summary>
        /// Escapes a string for use in a SQL query using the specified escape character.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <param name="escapeChar">The character to use for escaping.</param>
        /// <returns>The escaped string.</returns>
        protected static string BasicEscapeString(string value, char escapeChar)
        {
            StringBuilder builder = new StringBuilder(value.Length + 2).Append(escapeChar);

            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c == escapeChar) builder.Append(escapeChar);
                builder.Append(c);
            }

            return builder.Append(escapeChar).ToString();
        }
    }
}

using SharpOrm.DataTranslation;
using System;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Class responsible for loading query management information, its configurations, and translations.
    /// </summary>
    public abstract class QueryConfig : ICloneable
    {
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
        public ColumnTypeMap[] CustomColumnTypes { get; set; } = new ColumnTypeMap[0];

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
        /// If true, parameters will be used; if false, strings will be manually escaped.
        /// </summary>
        /// <remarks>
        /// Use this option with caution, as it can cause issues in the execution of your scripts.
        /// </remarks>
        public bool EscapeStrings { get; set; }

        /// <summary>
        /// Create an instance that allows only safe modifications.
        /// </summary>
        public QueryConfig() : this(true)
        {

        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="safeModificationsOnly">Signal whether only safe modifications should be made.</param>
        /// <remarks>Safe modifications are updates and deletes with a WHERE clause.</remarks>
        public QueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
        }

        /// <summary>
        /// Fix table name, column and alias for SQL.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract string ApplyNomenclature(string name);

        /// <summary>
        /// Creates a new grammar object.
        /// </summary>
        /// <param name="query">Query for grammar.</param>
        /// <returns></returns>
        public abstract Grammar NewGrammar(Query query);

        public virtual TableGrammar NewTableGrammar(TableSchema schema)
        {
            throw new NotSupportedException($"{this.GetType().FullName} does not support creating/editing/removing tables.");
        }

        public abstract string EscapeString(string value);

        object ICloneable.Clone() => this.Clone();

        public abstract QueryConfig Clone();

        protected void CopyTo(QueryConfig target)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var prop in this.GetType().GetProperties(flags))
                if (prop.CanRead && prop.CanWrite)
                    ReflectionUtils.CopyPropTo(this, target, prop);
        }
    }
}

using SharpOrm.SqlMethods;
using SharpOrm.SqlMethods.Mappers.Sqlite;

namespace SharpOrm.Builder
{
    public class SqliteQueryConfig : QueryConfig
    {
        public override bool CanUpdateJoin { get; } = false;

        public override SqlMethodRegistry Methods { get; } = new SqlMethodRegistry();

        /// <summary>
        /// Create an instance that allows only safe modifications.
        /// </summary>
        public SqliteQueryConfig()
        {
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="safeModificationsOnly">Signal whether only safe modifications should be made.</param>
        /// <remarks>Safe modifications are updates and deletes with a WHERE clause.</remarks>
        public SqliteQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        protected override void RegisterMethods()
        {
            Methods.Add(new SqliteStringMethods());
            Methods.Add(new SqliteDateProperties());
            Methods.Add(new SqliteDateMethods());
        }

        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('"', '"');
        }

        public override Grammar NewGrammar(Query query)
        {
            return new SqliteGrammar(query);
        }

        public override TableGrammar NewTableGrammar(TableSchema schema)
        {
            return new SqliteTableGrammar(this, schema);
        }

        /// <summary>
        /// Escapes a string for use in a MySQL query.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public override string EscapeString(string value) => BasicEscapeString(value, '\'');

        public override QueryConfig Clone(bool? safeOperations = null)
        {
            var clone = new SqliteQueryConfig(safeOperations ?? this.OnlySafeModifications);
            this.CopyTo(clone);
            return clone;
        }
    }
}

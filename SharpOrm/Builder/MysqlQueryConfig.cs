using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Mysql;
using SharpOrm.SqlMethods;
using SharpOrm.SqlMethods.Mappers.Mysql;
using System.Linq;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides configuration for building MySQL queries.
    /// </summary>
    public class MysqlQueryConfig : QueryConfig
    {
        private static string unsafeChars = "\\¥Š₩∖﹨＼\"'`\u00b4ʹʺʻʼˈˊˋ\u02d9\u0300\u0301‘’‚′‵❛❜＇";

        protected internal override bool NativeUpsertRows => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="MysqlQueryConfig"/> class.
        /// </summary>
        public MysqlQueryConfig()
        {
        }

        private MysqlQueryConfig(bool safeModificationsOnly, SqlMethodRegistry methods) : base(safeModificationsOnly, methods)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MysqlQueryConfig"/> class with a flag indicating if only safe modifications are allowed.
        /// </summary>
        /// <param name="safeModificationsOnly">If true, only safe modifications are allowed.</param>
        public MysqlQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        protected override void RegisterMethods()
        {
            Methods.Add(new MysqlStringMethods());
            Methods.Add(new MysqlDateProperties());
            Methods.Add(new MysqlDateMethods());
        }

        /// <summary>
        /// Creates a new <see cref="MysqlGrammar"/> for the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A new instance of <see cref="MysqlGrammar"/>.</returns>
        public override Grammar NewGrammar(Query query)
        {
            return new MysqlGrammar(query);
        }

        /// <summary>
        /// Applies MySQL-specific nomenclature to the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The sanitized name.</returns>
        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('`', '`');
        }

        /// <summary>
        /// Escapes a string for use in a MySQL query.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public override string EscapeString(string value) => Escape(value);

        /// <summary>
        /// Escapes a string for use in a MySQL query.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public static string Escape(string value)
        {
            StringBuilder build = new StringBuilder(value.Length + 2);
            build.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (IsUnsafe(c)) build.Append("\\");

                build.Append(c);
            }
            return build.Append('"').ToString();
        }

        /// <summary>
        /// Determines if a character is unsafe for use in a MySQL query.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>True if the character is unsafe; otherwise, false.</returns>
        public static bool IsUnsafe(char c)
        {
            return unsafeChars.Contains(c);
        }

        /// <summary>
        /// Creates a new <see cref="MysqlTableGrammar"/> for the specified schema.
        /// </summary>
        /// <param name="schema">The table schema.</param>
        /// <returns>A new instance of <see cref="MysqlTableGrammar"/>.</returns>
        public override TableGrammar NewTableGrammar(TableSchema schema)
        {
            return new MysqlTableGrammar(this, schema);
        }

        /// <summary>
        /// Clones the current configuration.
        /// </summary>
        /// <returns>A clone of the current configuration.</returns>
        public override QueryConfig Clone(bool? safeOperations = null)
        {
            var clone = new MysqlQueryConfig(safeOperations ?? this.OnlySafeModifications, Methods);
            this.CopyTo(clone);
            return clone;
        }
    }
}

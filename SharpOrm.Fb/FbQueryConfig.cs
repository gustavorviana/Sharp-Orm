using SharpOrm.Builder;
using System.Text;

namespace SharpOrm.Fb
{
    public class FbQueryConfig : QueryConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FbQueryConfig"/> class.
        /// </summary>
        public FbQueryConfig()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FbQueryConfig"/> class with a flag indicating if only safe modifications are allowed.
        /// </summary>
        /// <param name="safeModificationsOnly">If true, only safe modifications are allowed.</param>
        public FbQueryConfig(bool safeModificationsOnly) : base(safeModificationsOnly)
        {
        }

        /// <summary>
        /// Creates a new <see cref="FbGrammar"/> for the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A new instance of <see cref="FbGrammar"/>.</returns>
        public override Grammar NewGrammar(Query query)
        {
            return new FbGrammar(query);
        }

        /// <summary>
        /// Applies Fb-specific nomenclature to the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The sanitized name.</returns>
        public override string ApplyNomenclature(string name)
        {
            return name;
        }

        /// <summary>
        /// Escapes a string for use in a Firebird query.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public override string EscapeString(string value) => Escape(value);

        /// <summary>
        /// Escapes a string for use in a Firebird query.
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public static string Escape(string value)
        {
            StringBuilder build = new StringBuilder(value.Length + 2);
            build.Append('\'');
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (IsUnsafe(c)) build.Append('\'');

                build.Append(c);
            }
            return build.Append('\'').ToString();
        }

        /// <summary>
        /// Determines if a character is unsafe for use in a Firebird query.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>True if the character is unsafe; otherwise, false.</returns>
        public static bool IsUnsafe(char c)
        {
            return c == '\'';
        }

        /// <summary>
        /// Creates a new <see cref="FbTableGrammar"/> for the specified schema.
        /// </summary>
        /// <param name="schema">The table schema.</param>
        /// <returns>A new instance of <see cref="FbTableGrammar"/>.</returns>
        //public override TableGrammar NewTableGrammar(TableSchema schema)
        //{
        //    return new FbTableGrammar(this, schema);
        //}

        /// <summary>
        /// Clones the current configuration.
        /// </summary>
        /// <returns>A clone of the current configuration.</returns>
        public override QueryConfig Clone()
        {
            var clone = new FbQueryConfig(this.OnlySafeModifications);
            this.CopyTo(clone);
            return clone;
        }
    }
}
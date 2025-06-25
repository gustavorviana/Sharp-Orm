using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using SharpOrm.Fb.Grammars;
using SharpOrm.Fb.SqlMethods.Mappers;
using System.Text;

namespace SharpOrm.Fb
{
    public class FbQueryConfig : QueryConfig
    {
        protected internal override bool NativeUpsertRows => false;
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

        protected override void RegisterMethods()
        {
            Methods.Add(new FirebirdStringMethods());
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

        public override TableGrammar NewTableGrammar(TableSchema schema)
        {
            return new FbTableGrammar(this, schema);
        }

        /// <summary>
        /// Applies Firebird-specific nomenclature to the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The name with Firebird nomenclature applied (wrapped in double quotes).</returns>
        public override string ApplyNomenclature(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // For Firebird, we wrap identifiers in double quotes to handle case sensitivity
            // and special characters properly
            if (name.StartsWith("\"") && name.EndsWith("\""))
                return name;

            if (name == "*")
                return name;

            return name.SanitizeSqlName('"', '"');
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
        /// Clones the current configuration.
        /// </summary>
        /// <returns>A clone of the current configuration.</returns>
        public override QueryConfig Clone(bool? safeOperations = null)
        {
            var clone = new FbQueryConfig(safeOperations ?? OnlySafeModifications);
            this.CopyTo(clone);
            return clone;
        }
    }
}
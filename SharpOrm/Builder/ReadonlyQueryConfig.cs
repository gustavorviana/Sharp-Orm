using SharpOrm.Builder.Grammars;
using SharpOrm.SqlMethods.Mappers.SqlServer;
using System;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a read-only query configuration.
    /// </summary>
    internal class ReadonlyQueryConfig : QueryConfig
    {
        /// <summary>
        /// Returns the name without any treatment applied to it.
        /// </summary>
        /// <param name="name">The name to return.</param>
        /// <returns>The name without any treatment.</returns>
        public override string ApplyNomenclature(string name)
        {
            return name;
        }

        protected override void RegisterMethods()
        {
            Methods.Add(new SqlServerStringMethods());
            Methods.Add(new SqlServerDateProperties());
            Methods.Add(new SqlServerDateMethods());
        }

        public override string EscapeString(string value)
        {
            throw new NotImplementedException();
        }

        public override Grammar NewGrammar(Query query)
        {
            throw new NotImplementedException();
        }

        public override QueryConfig Clone(bool? safeOperations = null)
        {
            return new ReadonlyQueryConfig();
        }
    }
}

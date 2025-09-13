using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars.Table
{
    /// <summary>
    /// Defines a contract for building SQL CREATE INDEX statements.
    /// </summary>
    public interface IIndexSqlBuilder
    {
        /// <summary>
        /// Builds the SQL CREATE INDEX statement.
        /// </summary>
        /// <param name="indexDefinition">The index definition.</param>
        /// <returns>The SQL expression for creating the index.</returns>
        SqlExpression Build(IndexDefinition indexDefinition);
    }
}

using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;

namespace SharpOrm.Builder
{
    public interface ITableSchema : ICloneable
    {
        IReadOnlyList<Constraint> Constraints { get; }
        IReadOnlyList<IndexDefinition> Indexes { get; }
        IReadOnlyList<System.Data.DataColumn> Columns { get; }
        IMetadata Metadata { get; }

        string Name { get; }
        bool Temporary { get; }

        new ITableSchema Clone();
    }
}
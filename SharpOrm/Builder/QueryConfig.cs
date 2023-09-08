using SharpOrm.Builder.DataTranslation;
using System;

namespace SharpOrm.Builder
{
    public abstract class QueryConfig : IQueryConfig
    {
        /// <inheritdoc/>
        public DateTimeKind DateKind { get; set; }

        /// <inheritdoc/>
        public bool OnlySafeModifications { get; }

        /// <inheritdoc/>
        public int CommandTimeout { get; set; } = 30;

        /// <inheritdoc/>
        public bool ForeignLoader { get; set; }

        public QueryConfig() : this(true)
        {

        }

        public QueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
        }

        /// <inheritdoc/>
        public abstract string ApplyNomenclature(string name);

        /// <inheritdoc/>
        public TableReaderBase CreateTableReader(string[] tables, int maxDepth)
        {
            return new TableReader(this, tables, maxDepth) { CreateForeignIfNoDepth = this.ForeignLoader };
        }

        /// <inheritdoc/>
        public abstract Grammar NewGrammar(Query query);
    }
}

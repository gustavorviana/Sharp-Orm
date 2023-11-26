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
        [Obsolete("Use LoadForeignId instead. It will be removed in version 2.x.x.")]
        public bool ForeignLoader
        {
            get => this.LoadForeignId;
            set => this.LoadForeignId = value;
        }

        /// <inheritdoc/>
        public bool LoadForeignId { get; set; }

        /// <inheritdoc/>
        public TimeZoneInfo LocalTimeZone { get; set; } = TimeZoneInfo.Local;

        public QueryConfig() : this(true)
        {

        }

        public QueryConfig(bool safeModificationsOnly)
        {
            this.OnlySafeModifications = safeModificationsOnly;
        }

        /// <inheritdoc/>
        public abstract string ApplyNomenclature(string name);

        [Obsolete("It will be removed in version 2.x.x.")]
        public TableReaderBase CreateTableReader(string[] tables, int maxDepth)
        {
            return new TableReader(this, tables, maxDepth) { CreateForeignIfNoDepth = this.ForeignLoader };
        }

        /// <inheritdoc/>
        public abstract Grammar NewGrammar(Query query);
    }
}

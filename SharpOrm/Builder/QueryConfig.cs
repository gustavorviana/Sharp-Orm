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
        [Obsolete("Use LoadForeign instead. It will be removed in version 2.x.x.")]
        public bool ForeignLoader
        {
            get => this.LoadForeign;
            set => this.LoadForeign = value;
        }

        /// <inheritdoc/>
        public bool LoadForeign { get; set; }

        /// <inheritdoc/>
        [Obsolete("Use TranslationRegistry.Default.TimeZone instead. It will be removed in version 2.x.x.")]
        public TimeZoneInfo LocalTimeZone { get; set; } = TimeZoneInfo.Local;

        /// <summary>
        /// If true, parameters will be used; if false, strings will be manually escaped.
        /// </summary>
        /// <remarks>
        /// Use this option with caution, as it can cause issues in the execution of your scripts.
        /// </remarks>
        public bool EscapeStrings { get; set; }

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

        public abstract string EscapeString(string value);
    }
}

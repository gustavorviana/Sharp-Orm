using SharpOrm.Builder.Grammars.Sgbd.Mysql;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Provides the implementation for building SQL queries specific to MySQL using a fluent interface.
    /// </summary>
    public class MysqlGrammar : Grammar
    {
        public MysqlGrammar(Query query) : base(query)
        {
            this.builder.paramInterceptor += (original) =>
            {
                if (original is DateTimeOffset offset)
                    return TimeZoneInfo.ConvertTime(offset.UtcDateTime, GetTimeZoneInfo());

                return original;
            };
        }

        private TimeZoneInfo GetTimeZoneInfo()
        {
            return this.Info.Config.Translation.DbTimeZone;
        }

        protected override void ConfigureDelete()
        {
            new MysqlDeleteGrammar(this).Build();
        }

        protected override void ConfigureUpdate(IEnumerable<Cell> cells)
        {
            new MysqlUpdateGrammar(this).Build(cells);
        }

        protected override void ConfigureCount(Column column)
        {
            bool safeDistinct = (column == null || column.IsAll()) && this.Query.Distinct;

            if (safeDistinct)
                this.builder.Add("SELECT COUNT(*) FROM (");

            this.ConfigureSelect(true, safeDistinct ? null : column, true);

            if (safeDistinct)
                this.builder.Add(") ").Add(this.Info.Config.ApplyNomenclature("count"));
        }

        protected override void ConfigureSelect(bool configureWhereParams)
        {
            this.ConfigureSelect(configureWhereParams, null, false);
        }

        private void ConfigureSelect(bool configureWhereParams, Column countColumn, bool isCount)
        {
            bool _isCount = countColumn != null;
            bool isCustomCount = countColumn != null && countColumn.IsCount;

            this.builder.Add("SELECT ");

            if (_isCount && !isCustomCount)
                this.builder.Add("COUNT(");

            if (this.Query.Distinct && !isCustomCount)
                this.builder.Add("DISTINCT ");

            if (_isCount)
            {
                WriteSelect(countColumn);
                if (!isCustomCount)
                    this.builder.Add(')');
            }
            else
            {
                this.WriteSelectColumns();
            }

            this.builder.Add(" FROM ").Add(this.GetTableName(true));

            this.ApplyJoins();
            this.WriteWhere(configureWhereParams);

            this.WriteGroupBy();

            if (isCount || _isCount)
                return;

            if (this.CanWriteOrderby())
                this.ApplyOrderBy();

            this.WritePagination();
        }

        private void WritePagination()
        {
            if (this.Query.Limit is null && this.Query.Offset is null)
                return;

            this.builder.Add(" LIMIT ").Add(this.Query.Limit ?? int.MaxValue);

            if (this.Query.Offset != null)
                this.builder.Add(" OFFSET ").Add(this.Query.Offset);
        }
    }
}

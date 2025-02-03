using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder.Grammars
{
    public class MergeQueryInfo : IReadonlyQueryInfo
    {
        public QueryConfig Config { get; set; }

        public DbName TableName { get; set; }

        public string Alias { get; }

        internal MergeQueryInfo(DbName name, QueryConfig config, string aliasIfEmpty)
        {
            Config = config;

            TableName = string.IsNullOrEmpty(name.Alias) ?
                new DbName(name.Name, aliasIfEmpty) :
                name;

            Alias = Config.ApplyNomenclature(TableName.Alias);
        }

        public string GetFullName()
        {
            return TableName.GetName(true, Config);
        }
    }
}

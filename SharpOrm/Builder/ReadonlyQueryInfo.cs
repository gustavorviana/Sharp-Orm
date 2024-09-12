﻿using System;

namespace SharpOrm.Builder
{
    internal class ReadonlyQueryInfo : IReadonlyQueryInfo
    {
        public QueryConfig Config { get; }

        public DbName TableName { get; }

        public ReadonlyQueryInfo(QueryInfo info) : this(info.Config, info.TableName)
        {
        }

        public ReadonlyQueryInfo(QueryConfig config, DbName tableName)
        {
            this.Config = config ?? throw new ArgumentNullException("The query configuration cannot be null.");
            this.TableName = tableName;
        }
    }
}

using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.ForeignKey
{
    internal class FkColumn
    {
        public ColumnInfo ColumnInfo { get; }
        public Column Column { get; }
        public ForeignAttribute ForeignInfo => ColumnInfo?.ForeignInfo;

        public string Alias => Column.Alias;

        public FkColumn(ColumnInfo columnInfo, Column column)
        {
            ColumnInfo = columnInfo;
            Column = column;
        }
    }
}

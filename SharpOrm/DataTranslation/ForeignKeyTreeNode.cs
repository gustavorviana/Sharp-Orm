using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    internal class ForeignKeyTreeNode : ForeignKeyNodeBase
    {
        private readonly string _prefix;
        private List<FkColumn> _columns = new List<FkColumn>();
        public IReadOnlyList<FkColumn> Columns => _columns;

        public ColumnInfo ColumnInfo { get; }
        public TableInfo TableParent { get; }

        public MemberInfo Member => ColumnInfo.column;
        public string ParentKeyColumn => ColumnInfo.ForeignInfo?.ForeignKey;
        public string LocalKeyColumn => ColumnInfo.ForeignInfo?.LocalKey ?? "Id";

        public bool IsCollection { get; }

        public ForeignKeyTreeNode(TableInfo tableInfo, ColumnInfo columnInfo, TableInfo tableParent, string prefix, bool isCollection) : base(tableInfo)
        {
            ColumnInfo = columnInfo ?? throw new ArgumentNullException(nameof(columnInfo));
            IsCollection = isCollection;
            TableParent = tableParent;
            _prefix = prefix;

            LoadColumns();
        }

        private void LoadColumns()
        {
            var prefix = $"{GetTreePrefix()}c_";
            foreach (var column in TableInfo.Columns)
                _columns.Add(new FkColumn(column, new Column($"{TableInfo.Name}.{column.Name}", $"{prefix}{column.Name}")));
        }

        public IEnumerable<ForeignKeyTreeNode> GetAllNodes()
        {
            yield return this;

            foreach (var child in _nodes)
                foreach (var descendant in child.GetAllNodes())
                    yield return descendant;
        }

        public override string ToString()
        {
            if (TableParent == null)
                return $"{Member.Name} ({TableInfo.Name}) (Root)";

            var fkInfo = ColumnInfo?.ForeignInfo != null
                ? $"Parent Key: {ParentKeyColumn}, LK: {LocalKeyColumn}"
                : "No FK Info";

            return $"{Member.Name} ({TableInfo.Name}) -> {fkInfo}, Parent: {TableParent.Name}";
        }

        public override string GetTreePrefix()
        {
            return $"{_prefix}{base.GetTreePrefix()}";
        }

        public class FkColumn
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
}

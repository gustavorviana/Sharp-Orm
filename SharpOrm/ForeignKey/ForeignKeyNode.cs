using SharpOrm.Builder;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    internal class ForeignKeyNode : ForeignKeyNodeBase
    {
        public string Prefix { get; protected set; }
        private List<FkColumn> _columns = new List<FkColumn>();
        public IReadOnlyList<FkColumn> Columns => _columns;
        private IIncludable _includable;

        public ForeignKeyRegister Root { get; }
        public override ColumnInfo ColumnInfo { get; }
        public DbName TableParent { get; }
        public override DbName Name { get; }

        public MemberInfo Member => ColumnInfo.column;
        public string ParentKeyColumn => ColumnInfo.ForeignInfo?.ForeignKey;
        public string LocalKeyColumn => ColumnInfo.ForeignInfo?.LocalKey ?? "Id";

        public bool IsCollection { get; }
        public bool ParentIsCollection { get; }

        public override QueryInfo RootInfo => Root.RootInfo;

        public ForeignKeyNode(ForeignKeyRegister root, TableInfo tableInfo, ColumnInfo columnInfo, IForeignKeyNode parent, string prefix, bool isCollection) : base(tableInfo)
        {
            ColumnInfo = columnInfo ?? throw new ArgumentNullException(nameof(columnInfo));
            ParentIsCollection = CheckParentIsCollection(parent);
            Prefix = isCollection ? Member.Name : prefix + Member.Name;
            IsCollection = isCollection;
            TableParent = parent.Name;
            Name = BuildName();
            Root = root;

            LoadColumns();
        }

        private static bool CheckParentIsCollection(IForeignKeyNode parent)
        {
            return parent is ForeignKeyNode node && (node.IsCollection || node.ParentIsCollection);
        }

        private DbName BuildName()
        {
            if (TableInfo.Name == Prefix)
                return new DbName(TableInfo.Name, "");

            return new DbName(TableInfo.Name, Prefix);
        }

        private void LoadColumns()
        {
            var prefix = IsCollection ? string.Empty : $"{GetTreePrefix()}c_";
            foreach (var column in TableInfo.Columns)
                _columns.Add(new FkColumn(column, GetColumn(column, prefix)));
        }

        private Column GetColumn(ColumnInfo column, string prefix)
        {
            return new Column($"{Name.TryGetAlias()}.{column.Name}", $"{prefix}{column.Name}");
        }

        public override IEnumerable<Column> GetAllColumn()
        {
            foreach (var column in Columns)
                if (column.ForeignInfo == null && !ReflectionUtils.IsCollection(column.ColumnInfo.Type))
                    yield return column.Column;

            foreach (var node in GetAllChildNodes(false))
                if (!node.IsCollection)
                    foreach (var column in node.Columns)
                        if (column.ForeignInfo == null)
                            yield return column.Column;
        }

        public override string ToString()
        {
            var fkInfo = ColumnInfo?.ForeignInfo != null
                ? $"Parent Key: {ParentKeyColumn}, LK: {LocalKeyColumn}"
                : "No FK Info";

            return $"{Member.Name} ({TableInfo.Name}) -> {fkInfo}, Parent: {TableParent.Name}";
        }

        public override string GetTreePrefix()
        {
            return $"{Prefix}_";
        }

        internal Includable<TEntity, TProperty> GetIncludable<TEntity, TProperty>()
        {
            if (_includable is Includable<TEntity, TProperty> includable)
                return includable;

            includable = new Includable<TEntity, TProperty>(Root, this);
            _includable = includable;
            return includable;
        }

        protected override ForeignKeyNode CreateNode(ColumnInfo memberColumnInfo, TableInfo memberTableInfo, bool isCollection, bool silent = false)
        {
            var prefix = IsCollection ? string.Empty : GetTreePrefix();
            var node = new ForeignKeyNode(Root, memberTableInfo, memberColumnInfo, this, prefix, isCollection);
            if (!silent)
                ((INodeCreationListener)Root).Created(node);

            return node;
        }

        internal JoinQuery ToJoinQuery(QueryConfig config)
        {
            JoinQuery join = new JoinQuery(config, Name)
            {
                Type = "LEFT"
            };

            join.WhereColumn(
                $"{Name.TryGetAlias()}.{LocalKeyColumn}",
                "=",
                $"{TableParent.TryGetAlias()}.{ParentKeyColumn}"
            );

            return join;
        }
    }
}

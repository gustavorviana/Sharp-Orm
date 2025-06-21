using DbRunTest.Comparators;
using SharpOrm.Builder;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Base class for managing a collection of foreign key tree nodes associated with a table.
    /// </summary>
    internal abstract class ForeignKeyNodeBase : IForeignKeyNode
    {
        protected readonly List<ForeignKeyNode> _nodes = new List<ForeignKeyNode>();
        public IReadOnlyCollection<ForeignKeyNode> Nodes => _nodes;
        public TableInfo TableInfo { get; }
        public abstract DbName Name { get; }

        public abstract QueryInfo RootInfo { get; }

        public ColumnInfo ColumnInfo => null;

        public ForeignKeyNodeBase(TableInfo tableInfo)
        {
            TableInfo = tableInfo;
        }

        /// <summary>
        /// Gets an existing child node for the specified member or adds a new one if it does not exist.
        /// </summary>
        /// <param name="member">The member representing the foreign key relationship.</param>
        /// <returns>True if a new node was added; false if the node already existed.</returns>
        public virtual ForeignKeyNode GetOrAddChild(MemberInfo member, bool silent = false)
        {
            var node = Get(member);
            if (node != null)
                return node;

            var memberColumnInfo = TableInfo.GetColumn(member);
            if (memberColumnInfo == null)
                throw new InvalidOperationException($"Column not found for member {member.Name} in table {TableInfo.Name}");

            var memberTableInfo = TableInfo.registry.GetTable(GetMemberType(member, out var isCollection));
            node = CreateNode(memberColumnInfo, memberTableInfo, isCollection, silent);
            _nodes.Add(node);
            return node;
        }

        /// <summary>
        /// Gets the child node corresponding to the specified member, if it exists in the current collection.
        /// </summary>
        /// <param name="member">The member representing the foreign key relationship.</param>
        /// <returns>The <see cref="ForeignKeyNode"/> if found; otherwise, <c>null</c>.</returns>
        public virtual ForeignKeyNode Get(MemberInfo member)
        {
            return _nodes.FirstOrDefault(x => MemberInfoComparator.Default.Equals(x.Member, member));
        }

        protected abstract ForeignKeyNode CreateNode(ColumnInfo memberColumnInfo, TableInfo memberTableInfo, bool isCollection, bool silent);

        public bool Exists(MemberInfo member)
        {
            return _nodes.Any(x => x.Member.Equals(member));
        }

        private Type GetMemberType(MemberInfo member, out bool isCollection)
        {
            var memberType = TranslationRegistry.GetValidTypeFor(ReflectionUtils.GetMemberType(member));
            isCollection = RuntimeList.IsCollection(memberType);
            if (isCollection)
                return RuntimeList.GetCollectionElementType(memberType);

            return memberType;
        }

        public void ApplySelectToQuery(Query query)
        {
            if (!HasAnyNonCollection())
                return;

            var columns = GetAllColumn().ToArray();
            if (columns.Length > 0)
                query.Select(columns);
        }

        public bool HasAnyNonCollection()
        {
            foreach (var node in GetAllNodes())
                if (!node.IsCollection)
                    return true;

            return false;
        }

        public abstract string GetTreePrefix();

        public virtual IEnumerable<Column> GetAllColumn()
        {
            foreach (var item in TableInfo.Columns)
                if (item.ForeignInfo == null)
                    yield return new Column($"{Name.TryGetAlias()}.{item.Name}", "");

            foreach (var node in GetAllNodes())
                if (!node.IsCollection)
                    foreach (var column in node.Columns)
                        if (column.ForeignInfo == null)
                            yield return column.Column;
        }

        public virtual IEnumerable<ForeignKeyNode> GetAllNodes()
        {
            foreach (var child in _nodes)
                foreach (var descendant in child.GetAllNodes())
                    yield return descendant;
        }
    }
}

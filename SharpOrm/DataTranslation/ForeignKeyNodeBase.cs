using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Base class for managing a collection of foreign key tree nodes associated with a table.
    /// </summary>
    internal abstract class ForeignKeyNodeBase
    {
        protected readonly List<ForeignKeyTreeNode> _nodes = new List<ForeignKeyTreeNode>();
        public IReadOnlyCollection<ForeignKeyTreeNode> Nodes => _nodes;
        public TableInfo TableInfo { get; }

        public ForeignKeyNodeBase(TableInfo tableInfo)
        {
            TableInfo = tableInfo;
        }

        /// <summary>
        /// Gets an existing child node for the specified member or adds a new one if it does not exist.
        /// </summary>
        /// <param name="member">The member representing the foreign key relationship.</param>
        /// <param name="node">The resulting <see cref="ForeignKeyTreeNode"/> instance.</param>
        /// <returns>True if a new node was added; false if the node already existed.</returns>
        public virtual bool GetOrAddChild(MemberInfo member, out ForeignKeyTreeNode node)
        {
            node = _nodes.FirstOrDefault(x => x.Member.Equals(member));
            if (node != null)
                return false;

            var memberColumnInfo = TableInfo.GetColumn(member);
            if (memberColumnInfo == null)
                throw new InvalidOperationException($"Column not found for member {member.Name} in table {TableInfo.Name}");

            var memberTableInfo = TableInfo.registry.GetTable(GetMemberType(member, out var isCollection));

            node = new ForeignKeyTreeNode(memberTableInfo, memberColumnInfo, TableInfo, GetTreePrefix(), isCollection);
            _nodes.Add(node);

            return true;
        }

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

        public virtual string GetTreePrefix()
        {
            return $"{TableInfo.Name}_";
        }
    }
}

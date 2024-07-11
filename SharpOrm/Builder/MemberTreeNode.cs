using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal class MemberTreeNode
    {
        private ColumnMapInfo columnInfo;
        private readonly TranslationRegistry registry;

        public List<MemberTreeNode> Children { get; } = new List<MemberTreeNode>();
        public MemberInfo Member { get; }

        public MemberTreeNode(TranslationRegistry registry, MemberInfo member)
        {
            this.Member = member;
            this.registry = registry;
        }

        internal MemberTreeNode InternalFindChild(IList<MemberInfo> members, int offset)
        {
            if (members.Count <= offset) return null;

            var member = members[offset];
            foreach (var node in this.Children)
                if (members.Count == offset + 1 && node.Member == member) return node;
                else if (node.Member == member) return node.InternalFindChild(members, offset + 1);

            return null;
        }

        internal ColumnMapInfo GetColumn()
        {
            if (this.columnInfo != null) return this.columnInfo;

            if (this.Member is PropertyInfo prop) return this.columnInfo = new ColumnMapInfo(new ColumnInfo(registry, prop));
            if (this.Member is FieldInfo field) return this.columnInfo = new ColumnMapInfo(new ColumnInfo(registry, field));
            throw new NotSupportedException();
        }

        internal IEnumerable<ColumnTree> BuildTree(List<MemberInfo> root)
        {
            if (this.Children.Count == 0)
            {
                yield return this.Build(root);
                yield break;
            }

            foreach (var child in this.Children)
            {
                root.Add(child.Member);

                foreach (var result in child.BuildTree(root))
                    yield return result;

                root.RemoveAt(root.Count - 1);
            }
        }

        private ColumnTree Build(IEnumerable<MemberInfo> members)
        {
            this.GetColumn().builded = true;
            return new ColumnTree(this.columnInfo.column, members);
        }

        public override string ToString()
        {
            return this.columnInfo.column.Name;
        }
    }
}

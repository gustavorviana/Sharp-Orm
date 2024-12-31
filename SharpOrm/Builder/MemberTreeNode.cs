using SharpOrm.DataTranslation;
using System.Collections.Generic;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal class MemberTreeNode
    {
        private ColumnMapInfo columnInfo;

        public List<MemberTreeNode> Children { get; } = new List<MemberTreeNode>();
        public MemberInfo Member { get; }

        public MemberTreeNode(MemberInfo member)
        {
            this.Member = member;
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
            return this.columnInfo = new ColumnMapInfo(this.Member);
        }

        internal IEnumerable<ColumnTreeInfo> BuildTree(List<MemberInfo> root, TranslationRegistry registry)
        {
            if (this.Children.Count == 0)
            {
                yield return this.Build(root, registry);
                yield break;
            }

            foreach (var child in this.Children)
            {
                root.Add(child.Member);

                foreach (var result in child.BuildTree(root, registry))
                    yield return result;

                root.RemoveAt(root.Count - 1);
            }
        }

        private ColumnTreeInfo Build(List<MemberInfo> path, TranslationRegistry registry)
        {
            this.GetColumn().builded = true;

            if (string.IsNullOrEmpty(this.columnInfo._name))
                this.columnInfo._name = ReflectionUtils.ToPath(path, '_');

            return new ColumnTreeInfo(path, this.columnInfo, registry);
        }
    }
}

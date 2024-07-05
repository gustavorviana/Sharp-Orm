using SharpOrm.DataTranslation;
using System.Collections.Generic;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal class MemberTreeNode
    {
        public List<MemberTreeNode> Children { get; } = new List<MemberTreeNode>();
        public MemberInfo Member { get; }
        public string Name { get; set; }

        public MemberTreeNode(MemberInfo member)
        {
            this.Member = member;
            this.Name = Member?.Name;
        }

        public void AddChild(MemberTreeNode child)
        {
            Children.Add(child);
        }

        internal MemberTreeNode InternalFindChild(MemberInfo[] members, int offset)
        {
            if (members.Length <= offset) return null;

            var member = members[offset];
            foreach (var node in this.Children)
                if (members.Length == offset + 1 && node.Member == member) return node;
                else if (node.Member == member) return node.InternalFindChild(members, offset + 1);

            return null;
        }

        internal IEnumerable<ReflectedField> ToFieldTree(List<MemberInfo> root, TranslationRegistry registry)
        {
            if (this.Children.Count == 0)
            {
                yield return this.ToField(registry, root);
                yield break;
            }

            foreach (var child in this.Children)
            {
                root.Add(child.Member);

                foreach (var result in child.ToFieldTree(root, registry))
                    yield return result;

                root.RemoveAt(root.Count - 1);
            }
        }

        private ReflectedField ToField(TranslationRegistry registry, IEnumerable<MemberInfo> members)
        {
            return new ReflectedField(registry, this.Name, members);
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

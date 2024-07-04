using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    internal class MemberTreeNode
    {
        public MemberInfo Member { get; set; }
        public string Name { get; set; }
        public List<MemberTreeNode> Children { get; } = new List<MemberTreeNode>();

        public MemberTreeNode(MemberInfo member)
        {
            this.Member = member;
            this.Name = Member?.Name;
        }

        public void AddChild(MemberTreeNode child)
        {
            Children.Add(child);
        }

        internal MemberTreeNode InternalFindChild(string[] keys, int offset)
        {
            if (keys.Length <= offset) return null;

            string key = keys[offset];
            foreach (var node in this.Children)
                if (keys.Length == offset + 1 && node.Member.Name == key) return node;
                else if (node.Member?.Name == key) return node.InternalFindChild(keys, offset + 1);

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
            return new ReflectedField(registry, this.Member.Name, members);
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

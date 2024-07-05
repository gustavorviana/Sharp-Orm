using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder
{
    public class TableMap<T>
    {
        private static readonly BindingFlags Binding = BindingFlags.Instance | BindingFlags.Public;
        private readonly List<MemberTreeNode> Nodes = new List<MemberTreeNode>();
        public TranslationRegistry Registry { get; set; } = TranslationRegistry.Default;

        public TableMap()
        {
            foreach (var property in typeof(T).GetProperties(Binding))
                if (IsNative(property.PropertyType)) this.Nodes.Add(new MemberTreeNode(property));
                else this.Nodes.Add(Map(property));

            foreach (var field in typeof(T).GetFields(Binding))
                if (IsNative(field.FieldType)) this.Nodes.Add(new MemberTreeNode(field));
                else this.Nodes.Add(Map(field));
        }

        private MemberTreeNode Map(MemberInfo member)
        {
            var rootNode = new MemberTreeNode(member);
            var type = ReflectionUtils.GetMemberType(member);

            foreach (var property in type.GetProperties(Binding))
                if (IsNative(property.PropertyType)) rootNode.AddChild(new MemberTreeNode(property));
                else rootNode.AddChild(Map(property));

            foreach (var field in type.GetFields(Binding))
                if (IsNative(field.FieldType)) rootNode.AddChild(new MemberTreeNode(field));
                else rootNode.AddChild(Map(field));

            return rootNode;
        }

        public void Property(Expression<Func<T, object>> expression, string name)
        {
            var path = PropertyPathVisitor.GetPropertyPaths(expression).ToArray();
            if (path.Length == 0) throw new ArgumentException();

            if (path.Length == 1) this.SetMember(path[0], name);
            else if (this.FindByMember(path[0])?.InternalFindChild(path, 1) is MemberTreeNode node) node.Name = name;
        }

        private void SetMember(MemberInfo member, string name)
        {
            if (!IsNative(ReflectionUtils.GetMemberType(member)))
                throw new InvalidOperationException();

            this.FindByMember(member).Name = name;
        }

        private MemberTreeNode FindByMember(MemberInfo member)
        {
            return this.Nodes.FirstOrDefault(x => x.Member == member);
        }

        private static bool IsNative(Type type)
        {
            return TranslationUtils.IsNative(type, false) || ReflectionUtils.IsCollection(type);
        }

        public IEnumerable<ReflectedField> GetReflectedFields(TranslationRegistry registry)
        {
            List<MemberInfo> root = new List<MemberInfo>();

            foreach (var child in this.Nodes)
            {
                root.Add(child.Member);

                foreach (var result in child.ToFieldTree(root, registry))
                    yield return result;

                root.RemoveAt(root.Count - 1);
            }
        }

        private class PropertyPathVisitor : ExpressionVisitor
        {
            private readonly List<MemberInfo> members = new List<MemberInfo>();

            protected override Expression VisitMember(MemberExpression node)
            {
                members.Insert(0, node.Member);

                return base.VisitMember(node);
            }

            public static List<MemberInfo> GetPropertyPaths<K>(Expression<Func<K, object>> expression)
            {
                var visitor = new PropertyPathVisitor();
                visitor.Visit(expression);
                return visitor.members;
            }
        }
    }
}

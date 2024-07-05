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

        public TranslationRegistry Registry { get; }

        public TableMap(TranslationRegistry registry)
        {
            this.Registry = registry;

            foreach (var property in typeof(T).GetProperties(Binding))
                if (ColumnInfo.CanWork(property))
                    if (IsNative(property.PropertyType)) this.Nodes.Add(new MemberTreeNode(this.Registry, property));
                    else this.Nodes.Add(Map(property));

            foreach (var field in typeof(T).GetFields(Binding))
                if (ColumnInfo.CanWork(field))
                    if (IsNative(field.FieldType)) this.Nodes.Add(new MemberTreeNode(this.Registry, field));
                    else this.Nodes.Add(Map(field));
        }

        private MemberTreeNode Map(MemberInfo member)
        {
            var rootNode = new MemberTreeNode(this.Registry, member);
            var type = ReflectionUtils.GetMemberType(member);

            foreach (var property in type.GetProperties(Binding))
                if (ColumnInfo.CanWork(property))
                    if (IsNative(property.PropertyType)) rootNode.AddChild(new MemberTreeNode(this.Registry, property));
                    else rootNode.AddChild(Map(property));

            foreach (var field in type.GetFields(Binding))
                if (ColumnInfo.CanWork(field))
                    if (IsNative(field.FieldType)) rootNode.AddChild(new MemberTreeNode(this.Registry, field));
                    else rootNode.AddChild(Map(field));

            return rootNode;
        }

        public ColumnMapInfo Property(Expression<Func<T, object>> expression, string columnName)
        {
            return this.Property(expression).SetColumn(columnName);
        }

        public ColumnMapInfo Property(Expression<Func<T, object>> expression)
        {
            var path = PropertyPathVisitor.GetPropertyPaths(expression);
            if (path.Count == 0) throw new ArgumentOutOfRangeException(nameof(expression), "At least one field must be selected.");

            var root = this.Nodes.FirstOrDefault(x => x.Member == path[0]) ?? throw new ArgumentOutOfRangeException();

            if (path.Count > 1)
                return root.InternalFindChild(path, 1).GetColumn() ?? throw new ArgumentOutOfRangeException();

            Type memberType = ReflectionUtils.GetMemberType(root.Member);
            if (!IsNative(memberType))
                throw new InvalidOperationException($"It is not possible to map the member \"{root.Member}\" of type \"{memberType}\".");

            return root.GetColumn();
        }

        private static bool IsNative(Type type)
        {
            return TranslationUtils.IsNative(type, false) || ReflectionUtils.IsCollection(type);
        }

        public IEnumerable<ColumnTree> GetFields()
        {
            List<MemberInfo> root = new List<MemberInfo>();

            foreach (var child in this.Nodes)
            {
                root.Add(child.Member);

                foreach (var result in child.BuildTree(root))
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

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                throw new ArgumentException("Only properties and fields are supported in this operation.");
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

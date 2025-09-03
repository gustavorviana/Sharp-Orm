using SharpOrm.DataTranslation;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder.Tables.Loaders
{
    internal class ColumnLoader<T> : ColumnLoader
    {
        public ColumnLoader(TranslationRegistry registry, NestedMode nestedMode) : base(typeof(T), registry, nestedMode)
        {
        }

        public ColumnLoader(TranslationRegistry registry) : base(typeof(T), registry)
        {
        }

        internal MemberTreeNode GetColumnFromExpression(Expression<Func<T, object>> expression, bool needNative, out MemberTreeNode root)
        {
            return base.GetColumnFromExpression(expression, needNative, out root);
        }
    }

    internal class ColumnLoader : IColumnLoader
    {
        private ColumnCollection _columns;
        private readonly Type _type;

        internal List<MemberTreeNode> Nodes { get; } = new List<MemberTreeNode>();
        public NestedMode NestedMode { get; }

        /// <summary>
        /// Gets the translation registry used for the mapping.
        /// </summary>
        public TranslationRegistry Registry { get; }

        public ColumnLoader(Type type, TranslationRegistry registry) : this(type, registry, registry.NestedMapMode)
        {

        }

        public ColumnLoader(Type type, TranslationRegistry registry, NestedMode nestedMode)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            NestedMode = nestedMode;

            LoadNodes();
        }

        private void LoadNodes()
        {
            foreach (var property in _type.GetProperties(Bindings.PublicInstance))
                if (MemberTreeNode.MapNode(property, NestedMode) is MemberTreeNode node)
                    Nodes.Add(node);

            foreach (var member in _type.GetMembers(Bindings.PublicInstance))
                if (MemberTreeNode.MapNode(member, NestedMode) is MemberTreeNode node)
                    Nodes.Add(node);
        }

        public ColumnCollection LoadColumns()
        {
            if (_columns != null)
                return _columns;

            _columns = new ColumnCollection();

            List<MemberInfo> root = new List<MemberInfo>();

            foreach (var child in Nodes)
            {
                root.Add(child.Member);
                child.BuildTree(root, _columns, Registry, string.Empty);
                root.RemoveAt(root.Count - 1);
            }

            return _columns.Build();
        }

        internal MemberTreeNode GetOrAdd(MemberInfo member)
        {
            var root = Nodes.FirstOrDefault(x => x.Member == member);
            if (root != null)
                return root;

            root = new MemberTreeNode(member, member.GetCustomAttribute<MapNestedAttribute>());
            Nodes.Add(root);
            return root;
        }

        protected static void ValidateNonNative(bool needNative, MemberTreeNode root)
        {
            Type memberType = ReflectionUtils.GetMemberType(root.Member);
            if (needNative && !TranslationUtils.IsNative(memberType, false) && !ReflectionUtils.IsCollection(memberType))
                throw new InvalidOperationException(string.Format(Messages.Table.MemberTypeNotSupported, root.Member, memberType));
        }

        internal MemberTreeNode GetColumnFromExpression(Expression expression, bool needNative, out MemberTreeNode root)
        {
            var path = PropertyPathVisitor.GetPropertyPaths(expression);
            if (path.Count == 0) throw new ArgumentOutOfRangeException(nameof(expression), "At least one field must be selected.");

            root = GetOrAdd(path[0]);
            for (int i = 1; i < path.Count; i++)
                root = root.GetOrAdd(path[i]);

            ValidateNonNative(needNative, root);
            return root;
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
                throw new ArgumentException(Messages.Expressions.OnlyFieldsAndproerties);
            }

            public static List<MemberInfo> GetPropertyPaths(Expression expression)
            {
                var visitor = new PropertyPathVisitor();
                visitor.Visit(expression);
                return visitor.members;
            }
        }
    }
}

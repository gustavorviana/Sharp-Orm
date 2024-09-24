using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents a mapping between a type <typeparamref name="T"/> and a database table.
    /// </summary>
    /// <typeparam name="T">The type to be mapped to the table.</typeparam>
    public class TableMap<T>
    {
        private static readonly BindingFlags Binding = BindingFlags.Instance | BindingFlags.Public;
        private readonly List<MemberTreeNode> Nodes = new List<MemberTreeNode>();
        private TableInfo table;

        private string _name;
        /// <summary>
        /// Gets or sets the name of the table. 
        /// Throws an <see cref="InvalidOperationException"/> if the table has already been created.
        /// </summary>
        public string Name
        {
            get => this._name;
            set
            {
                if (this.table != null)
                    throw new InvalidOperationException("It is not possible to change the table name after it has been created.");

                this._name = value;
            }
        }

        /// <summary>
        /// Gets the translation registry used for the mapping.
        /// </summary>
        public TranslationRegistry Registry { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableMap{T}"/> class.
        /// Automatically maps the properties and fields of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="registry">The translation registry to be used for the mapping.</param>
        public TableMap(TranslationRegistry registry)
        {
            this.Name = typeof(T).Name;
            this.Registry = registry;

            foreach (var property in typeof(T).GetProperties(Binding))
                if (ColumnInfo.CanWork(property))
                    if (IsNative(property.PropertyType)) this.Nodes.Add(new MemberTreeNode(property));
                    else this.Nodes.Add(Map(property));

            foreach (var field in typeof(T).GetFields(Binding))
                if (ColumnInfo.CanWork(field))
                    if (IsNative(field.FieldType)) this.Nodes.Add(new MemberTreeNode(field));
                    else this.Nodes.Add(Map(field));
        }

        private MemberTreeNode Map(MemberInfo member)
        {
            var rootNode = new MemberTreeNode(member);
            var type = ReflectionUtils.GetMemberType(member);

            foreach (var property in type.GetProperties(Binding))
                if (ColumnInfo.CanWork(property))
                    if (IsNative(property.PropertyType)) rootNode.Children.Add(new MemberTreeNode(property));
                    else rootNode.Children.Add(Map(property));

            foreach (var field in type.GetFields(Binding))
                if (ColumnInfo.CanWork(field))
                    if (IsNative(field.FieldType)) rootNode.Children.Add(new MemberTreeNode(field));
                    else rootNode.Children.Add(Map(field));

            return rootNode;
        }

        /// <summary>
        /// Sets the key property for the table using the specified expression.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be used as the key.</param>
        /// <returns>The current <see cref="TableMap{T}"/> instance.</returns>
        public TableMap<T> HasKey(Expression<Func<T, object>> expression)
        {
            this.Property(expression).SetKey(true);
            return this;
        }

        /// <summary>
        /// Maps a property to a specific column name in the table.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be mapped.</param>
        /// <param name="columnName">The name of the column in the table.</param>
        /// <returns>A <see cref="ColumnMapInfo"/> representing the mapped column.</returns>
        public ColumnMapInfo Property(Expression<Func<T, object>> expression, string columnName)
        {
            return this.Property(expression).HasColumnName(columnName);
        }

        /// <summary>
        /// Maps a property using the specified expression.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be mapped.</param>
        /// <returns>A <see cref="ColumnMapInfo"/> representing the mapped property.</returns>
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

        internal IEnumerable<ColumnTreeInfo> GetFields()
        {
            List<MemberInfo> root = new List<MemberInfo>();

            foreach (var child in this.Nodes)
            {
                root.Add(child.Member);

                foreach (var result in child.BuildTree(root, Registry))
                    yield return result;

                root.RemoveAt(root.Count - 1);
            }
        }

        /// <summary>
        /// Builds the table mapping and registers it with the translation registry.
        /// </summary>
        /// <returns>A <see cref="TableInfo"/> representing the built table mapping.</returns>
        public TableInfo Build()
        {
            if (this.Nodes.Count == 0) return null;
            if (this.table != null) return this.table;

            return this.table = this.Registry.AddTableMap(this);
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

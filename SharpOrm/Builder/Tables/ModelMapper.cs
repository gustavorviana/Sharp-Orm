using SharpOrm.Builder.Tables;
using SharpOrm.DataTranslation;
using SharpOrm.Msg;
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
    public class ModelMapper<T> : IModelMapper
    {
        private readonly List<MemberTreeNode> Nodes = new List<MemberTreeNode>();
        private TableInfo table;

        internal SoftDeleteAttribute _softDelete { get; set; }
        internal HasTimestampAttribute _timestamp { get; set; }

        private string _name;
        /// <summary>
        /// Gets or sets the name of the table. 
        /// Throws an <see cref="InvalidOperationException"/> if the table has already been created.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (table != null)
                    throw new InvalidOperationException(Messages.Table.CannotChangeAfterBuild);

                _name = value;
            }
        }

        /// <summary>
        /// Gets the translation registry used for the mapping.
        /// </summary>
        public TranslationRegistry Registry { get; }

        public ModelMapper(QueryConfig config) : this(config.Translation, config.NestedMapMode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelMapper{T}"/> class.
        /// Automatically maps the properties and fields of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="registry">The translation registry to be used for the mapping.</param>
        public ModelMapper(TranslationRegistry registry, NestedMode nestedMode = NestedMode.All)
        {
            Name = typeof(T).Name;
            Registry = registry;

            foreach (var property in typeof(T).GetProperties(Bindings.PublicInstance))
                if (ColumnInfo.CanWork(property))
                    if (MemberTreeNode.IsNative(property.PropertyType)) Nodes.Add(new MemberTreeNode(property));
                    else if (nestedMode == NestedMode.All) Nodes.Add(new MemberTreeNode(property).MapChildren(property, true));

            foreach (var field in typeof(T).GetFields(Bindings.PublicInstance))
                if (ColumnInfo.CanWork(field))
                    if (MemberTreeNode.IsNative(field.FieldType)) Nodes.Add(new MemberTreeNode(field));
                    else if (nestedMode == NestedMode.All) Nodes.Add(new MemberTreeNode(field).MapChildren(field, true));
        }

        public ModelMapper<T> ApplyConfiguration(IModelMapperConfiguration<T> configuration)
        {
            configuration.Configure(this);
            return this;
        }

        public ModelMapper<T> SoftDelete(string column, string dateColumn = null)
        {
            _softDelete = new SoftDeleteAttribute(column) { DateColumnName = dateColumn };
            return this;
        }

        public ModelMapper<T> HasTimeStamps(string createdAtColumn, string updatedAtColumn)
        {
            _timestamp = new HasTimestampAttribute { CreatedAtColumn = createdAtColumn, UpdatedAtColumn = updatedAtColumn };
            return this;
        }

        /// <summary>
        /// Sets the key property for the table using the specified expression.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be used as the key.</param>
        /// <returns>The current <see cref="ModelMapper{T}"/> instance.</returns>
        public ModelMapper<T> HasKey(Expression<Func<T, object>> expression)
        {
            Property(expression).SetKey(true);
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
            return Property(expression).HasColumnName(columnName);
        }

        /// <summary>
        /// Maps a property using the specified expression.
        /// </summary>
        /// <param name="expression">An expression selecting the property to be mapped.</param>
        /// <returns>A <see cref="ColumnMapInfo"/> representing the mapped property.</returns>
        public ColumnMapInfo Property(Expression<Func<T, object>> expression)
        {
            return GetColumnFromExpression(expression, true, out _)?.GetColumn(Registry) ?? throw new InvalidOperationException(Messages.Expressions.Invalid);
        }

        /// <summary>
        /// Maps a nested property with a specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to use for the nested property.</param>
        /// <returns></returns>
        public ModelMapper<T> MapNested(Expression<Func<T, object>> expression, string prefix = null, bool subNested = false)
        {
            var column = GetColumnFromExpression(expression, false, out _);
            column.MapChildren(column.Member, subNested);
            column.prefix = prefix;
            return this;
        }

        private MemberTreeNode GetColumnFromExpression(Expression<Func<T, object>> expression, bool needNative, out MemberTreeNode root)
        {
            var path = PropertyPathVisitor.GetPropertyPaths(expression);
            if (path.Count == 0) throw new ArgumentOutOfRangeException(nameof(expression), "At least one field must be selected.");

            root = GetOrAddRoot(path[0]);
            if (path.Count != 1)
                return GetNode(root, path);

            ValidateNonNative(needNative, root);
            return root;
        }

        private static MemberTreeNode GetNode(MemberTreeNode node, List<MemberInfo> path)
        {
            for (int i = 1; i < path.Count; i++)
                node = node.GetOrAdd(path[i]);

            return node;
        }

        internal IEnumerable<ColumnTreeInfo> GetFields()
        {
            List<MemberInfo> root = new List<MemberInfo>();

            foreach (var child in Nodes)
            {
                root.Add(child.Member);

                foreach (var result in child.BuildTree(root, Registry, null))
                    yield return result;

                root.RemoveAt(root.Count - 1);
            }
        }

        private static void ValidateNonNative(bool needNative, MemberTreeNode root)
        {
            Type memberType = ReflectionUtils.GetMemberType(root.Member);
            if (needNative && !MemberTreeNode.IsNative(memberType))
                throw new InvalidOperationException(string.Format(Messages.Table.MemberTypeNotSupported, root.Member, memberType));
        }

        private MemberTreeNode GetOrAddRoot(MemberInfo member)
        {
            var root = Nodes.FirstOrDefault(x => x.Member == member);
            if (root != null)
                return root;

            root = new MemberTreeNode(member);
            Nodes.Add(root);
            return root;
        }

        /// <summary>
        /// Builds the table mapping and registers it with the translation registry.
        /// </summary>
        /// <returns>A <see cref="TableInfo"/> representing the built table mapping.</returns>
        public TableInfo Build()
        {
            if (Nodes.Count == 0) return null;
            if (table != null) return table;

            return table = Registry.AddTableMap(this);
        }

        public Column GetColumn(Expression<ColumnExpression<T>> columnExpression)
        {
            if (table == null)
                throw new Exception(Messages.Table.NotBuilded);

            return Column.FromExp(columnExpression, Registry);
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

            public static List<MemberInfo> GetPropertyPaths<K>(Expression<Func<K, object>> expression)
            {
                var visitor = new PropertyPathVisitor();
                visitor.Visit(expression);
                return visitor.members;
            }
        }
    }
}

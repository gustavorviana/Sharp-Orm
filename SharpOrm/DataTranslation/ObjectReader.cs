using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.Builder.Tables;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Class responsible for reading objects and translating them into rows.
    /// </summary>
    public class ObjectReader : ObjectReaderBase
    {
        private IColumnsFilter _filter = new DefaultFilter();
        private ColumnCollection _columns;
        private bool _hasPendingChanges;

        private readonly bool _hasUpdateColumn;
        private readonly bool _hasCreateColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReader"/> class.
        /// </summary>
        /// <param name="table">The table information.</param>
        public ObjectReader(TableInfo table) : base(table)
        {
            _columns = table.Columns;
            _hasUpdateColumn = !string.IsNullOrEmpty(table.Timestamp?.UpdatedAtColumn);
            _hasCreateColumn = !string.IsNullOrEmpty(table.Timestamp?.CreatedAtColumn);
        }

        /// <summary>
        /// Creates an <see cref="ObjectReader"/> for the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="registry">The translation registry.</param>
        /// <returns>An <see cref="ObjectReader"/> for the specified type.</returns>
        [Obsolete("This method is obsolete and will be removed in version 4.0. Use IObjectReaderFactory.OfType() instead.")]
        public static ObjectReaderBase OfType<T>(TranslationRegistry registry)
        {
            return Create<T>(registry);
        }

        #region ObjectReader

        /// <summary>  
        /// Gets the names of the columns that are allowed to be read.  
        /// </summary>  
        /// <returns>An array of column names.</returns>  
        public override string[] GetColumnNames()
        {
            ApplyPendingChanges();
            List<string> names = new List<string>();
            names.AddRange(_columns.Where(column => !IsTimeStamps(column.Name)).Select(x => x.Name));
            if (Settings.IgnoreTimestamps)
                return names.ToArray();

            if (_hasCreateColumn && Settings.IsCreate)
                names.Add(_table.Timestamp.CreatedAtColumn);

            if (_hasUpdateColumn)
                names.Add(_table.Timestamp.UpdatedAtColumn);

            return names.ToArray();
        }

        /// <summary>
        /// Reads the cells from the specified object.
        /// </summary>
        /// <param name="owner">The object to read.</param>
        /// <returns>An enumerable of cells representing the object.</returns>
        public override IEnumerable<Cell> ReadCells(object owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (!ReflectionUtils.SameType(owner.GetType(), _table.Type))
                throw new ArgumentException($"The type of the provided object ({owner.GetType().FullName}) does not match the expected type ({_table.Type.FullName}).", nameof(owner));

            ApplyPendingChanges();
            var visited = new HashSet<object>();
            var builder = new RowBuilder { OverrideOnAdd = true };
            ReadObjectCells(visited, builder, owner);

            return builder.GetCells();
        }

        private void ReadObjectCells(HashSet<object> visited, RowBuilder builder, object owner)
        {
            if (visited.Contains(owner))
                return;

            visited.Add(owner);

            var context = new ValidationContext(owner);

            foreach (var node in _columns.Nodes)
                ReadObjectCells(context, builder, node);

            if (!Settings.IgnoreTimestamps && _hasCreateColumn && Settings.IsCreate)
                builder.Add(_table.Timestamp.CreatedAtColumn, DateTime.UtcNow);

            if (!Settings.IgnoreTimestamps && _hasUpdateColumn)
                builder.Add(_table.Timestamp.UpdatedAtColumn, DateTime.UtcNow);
        }

        private void ReadObjectCells(ValidationContext context, RowBuilder builder, IColumnNode node)
        {
            if (node.Nodes.Count == 0)
            {
                if (!IsTimeStamps(node.Column.Name) && GetCell(context, node.Column) is Cell cell)
                    builder.Add(cell);

                return;
            }

            context = new ValidationContext(GetRawValue(node.Column, context.ObjectInstance));

            foreach (var childNode in node.Nodes)
                ReadObjectCells(context, builder, childNode);
        }

        private Cell GetCell(ValidationContext context, ColumnInfo column)
        {
            if (column.ForeignInfo != null)
                return new Cell(column.ForeignInfo.ForeignKey, GetFkValue(context.ObjectInstance, GetRawValue(column, context.ObjectInstance), column));

            object value = ProcessValue(column, context.ObjectInstance);
            if (column.Key && !CanUseKeyValue(value))
                return null;

            if (Settings.Validate) column.ValidateValue(context, value);
            return new Cell(column.Name, value);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var type = GetValidType(fkColumn.Type);
            if (type == typeof(Row))
                return null;

            var table = GetTable(type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(GetRawValue(fkColumn, owner) is object fkInstance))
                return null;

            return GetValue(pkColumn, fkInstance);
        }

        private static Type GetValidType(Type type)
        {
            return ReflectionUtils.IsCollection(type) ? ReflectionUtils.GetGenericArg(type) : type;
        }
        #endregion

        private bool IsTimeStamps(string name)
        {
            var timestamps = _table.Timestamp;
            return (_hasUpdateColumn && name.Equals(timestamps.UpdatedAtColumn, StringComparison.OrdinalIgnoreCase)) ||
                (_hasCreateColumn && name.Equals(timestamps.CreatedAtColumn, StringComparison.OrdinalIgnoreCase));
        }

        protected override void SetExpression<K>(Expression<ColumnExpression<K>> expression, bool needContains)
        {
            _filter = new MemberColumnFilter(GetMembers(expression).ToArray(), needContains);
            _hasPendingChanges = true;
        }

        protected override void SetColumns(string[] columns, bool needContains)
        {
            _filter = new NamesFilter(columns, needContains);
        }

        protected override void OnCriteriaChange()
        {
            _hasPendingChanges = true;
        }

        private void ApplyPendingChanges()
        {
            if (!_hasPendingChanges)
                return;

            _hasPendingChanges = false;
            _columns = _filter.FilterColumns(this, _table.Columns);
        }

        #region IColumnFilter
        private interface IColumnsFilter
        {
            ColumnCollection FilterColumns(ObjectReader reader, ColumnCollection columns);
        }

        private class DefaultFilter : IColumnsFilter
        {
            public ColumnCollection FilterColumns(ObjectReader reader, ColumnCollection columns)
            {
                var builder = new ColumnCollectionBuilder();

                foreach (var node in columns.Nodes.Cast<ColumnCollection.ColumnNode>())
                    AddNodeToBuilder(reader, builder, node);

                return builder.Build();
            }

            private void AddNodeToBuilder(ObjectReader reader, ITreeAdd<ColumnCollectionBuilder.BuilderNode> builder, ColumnCollection.ColumnNode sourceNode)
            {
                if (sourceNode.Nodes.Length == 0)
                {
                    if (reader.CanReadColumn(sourceNode.Column) && CanUse(sourceNode))
                        builder.Add(sourceNode.Column);

                    return;
                }

                var parentBuilder = builder.Add(sourceNode.Column);
                foreach (var childNode in sourceNode.Nodes.Cast<ColumnCollection.ColumnNode>())
                    AddNodeToBuilder(reader, parentBuilder, childNode);
            }

            protected virtual bool CanUse(IColumnNode node)
            {
                return true;
            }
        }

        private class NamesFilter : DefaultFilter
        {
            private readonly string[] _names;
            private readonly bool _needContains;

            public NamesFilter(string[] names, bool needContains)
            {
                _names = names ?? DotnetUtils.EmptyArray<string>();
                _needContains = needContains;
            }

            protected override bool CanUse(IColumnNode node)
            {
                return _names.Length == 0 || _names.Contains(node.Column.Name, StringComparer.OrdinalIgnoreCase) == _needContains;
            }
        }

        private class MemberColumnFilter : IColumnsFilter
        {
            private readonly SqlMember[] _members;
            private readonly bool _needContains;

            public MemberColumnFilter(SqlMember[] members, bool needContains)
            {
                _members = members;
                _needContains = needContains;
            }

            public ColumnCollection FilterColumns(ObjectReader reader, ColumnCollection columns)
            {
                if (_needContains)
                    return FilterNeededContainsColumn(reader, columns);

                return MakeBuilder(reader, columns).Build();
            }

            private ColumnCollection FilterNeededContainsColumn(ObjectReader reader, ColumnCollection columns)
            {
                var builder = new ColumnCollectionBuilder();
                ITreeAdd<ColumnCollectionBuilder.BuilderNode> target = builder;

                foreach (var member in _members)
                {
                    IWithColumnNode node = GetRootNode(member, columns);
                    if (node is IColumnNode info)
                        target = target.Add(info.Column);

                    for (int i = 1; i < member.Path.Length; i++)
                    {
                        node = node.Nodes.First(x => x.Column.PropName == member.Path[0].Name);
                        target = target.Add((node as IColumnNode).Column);
                    }

                    var column = (node.Nodes.First(x => x.Column.PropName == member.Name) as IColumnNode).Column;
                    if (reader.CanReadColumn(column))
                        target.Add(column);
                }

                return builder.Build();
            }

            private IWithColumnNode GetRootNode(SqlMember member, ColumnCollection columns)
            {
                if (member.Path.Length == 0)
                    return columns;

                return columns.Nodes.First(x => x.Column.PropName == member.Path[0].Name);
            }

            private ColumnCollectionBuilder MakeBuilder(ObjectReader reader, ColumnCollection columns)
            {
                var builder = new ColumnCollectionBuilder();

                foreach (var node in columns.Nodes)
                    CopyNode(reader, builder, node);

                foreach (var member in _members)
                    builder.Remove(member);

                return builder;
            }

            private void CopyNode(ObjectReader reader, ITreeAdd<ColumnCollectionBuilder.BuilderNode> target, IColumnNode node)
            {
                if (node.Nodes.Count == 0 && !reader.CanReadColumn(node.Column))
                    return;

                target = target.Add(node.Column);

                foreach (var childNode in node.Nodes)
                    CopyNode(reader, target, childNode);
            }
        }
        #endregion
    }
}

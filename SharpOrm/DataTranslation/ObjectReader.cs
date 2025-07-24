using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Class responsible for reading objects and translating them into rows.
    /// </summary>
    public class ObjectReader : ObjectReaderBase
    {
        private ColumnInfo[] _columns;
        private ICachedColumnLoader _cachedColumns = new DefaultCachedColumnLoader();
        private bool _hasPendingChanges;


        private readonly bool _hasUpdateColumn;
        private readonly bool _hasCreateColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReader"/> class.
        /// </summary>
        /// <param name="table">The table information.</param>
        public ObjectReader(TableInfo table) : base(table)
        {
            _columns = GetValidColumns().ToArray();
            _hasUpdateColumn = !string.IsNullOrEmpty(table.Timestamp?.UpdatedAtColumn);
            _hasCreateColumn = !string.IsNullOrEmpty(table.Timestamp?.CreatedAtColumn);
        }

        /// <summary>
        /// Creates an <see cref="ObjectReader"/> for the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="registry">The translation registry.</param>
        /// <returns>An <see cref="ObjectReader"/> for the specified type.</returns>
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
            if (IgnoreTimestamps)
                return names.ToArray();

            if (_hasCreateColumn && IsCreate)
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
            ApplyPendingChanges();
            return ReadObjectCells(new ValidationContext(owner));
        }

        private IEnumerable<Cell> ReadObjectCells(ValidationContext context)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (IsTimeStamps(_columns[i].Name))
                    continue;

                var cell = GetCell(context, _columns[i]);
                if (cell != null) yield return cell;
            }

            if (_hasCreateColumn && IsCreate)
                yield return new Cell(_table.Timestamp.CreatedAtColumn, DateTime.UtcNow);

            if (_hasUpdateColumn)
                yield return new Cell(_table.Timestamp.UpdatedAtColumn, DateTime.UtcNow);
        }

        private Cell GetCell(ValidationContext context, ColumnInfo column)
        {
            if (column.ForeignInfo != null)
                return new Cell(column.ForeignInfo.ForeignKey, GetFkValue(context.ObjectInstance, column.GetRaw(context.ObjectInstance), column));

            object value = ProcessValue(column, context.ObjectInstance);
            if (column.Key && !CanUseKeyValue(value))
                return null;

            if (Validate) column.ValidateValue(context, value);
            return new Cell(column.Name, value);
        }

        private object GetFkValue(object owner, object value, ColumnInfo fkColumn)
        {
            var type = GetValidType(fkColumn.Type);
            if (type == typeof(Row))
                return null;

            var table = GetTable(type);
            var pkColumn = table.Columns.First(c => c.Key);

            if (TranslationUtils.IsInvalidPk(value) || !(fkColumn.GetRaw(owner) is object fkInstance))
                return null;

            return pkColumn.Get(fkInstance);
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
            _cachedColumns = new CachedMemberColumnLoader(GetMembers(expression).Select(x => x.Member).ToArray(), needContains);
            _hasPendingChanges = true;
        }

        protected override void SetColumns(string[] columns, bool needContains)
        {
            _cachedColumns = new CachedNameColumnLoader(columns, needContains);
            _hasPendingChanges = true;
        }

        protected override void OnCriterioChange()
        {
            _hasPendingChanges = true;
        }

        private void ApplyPendingChanges()
        {
            if (!_hasPendingChanges || _cachedColumns == null)
                return;

            _hasPendingChanges = false;
            _columns = _cachedColumns.GetColumns(GetValidColumns());
        }

        private class CachedNameColumnLoader : ICachedColumnLoader
        {
            private readonly bool _needContains;
            private readonly string[] _columns;

            public CachedNameColumnLoader(string[] columns, bool needContains)
            {
                _columns = columns;
                _needContains = needContains;
            }

            public ColumnInfo[] GetColumns(IEnumerable<ColumnInfo> columns)
            {
                if (_columns?.Length > 0)
                    return columns.Where(x => _columns.ContainsIgnoreCase(x.Name) == _needContains).ToArray();

                return columns.ToArray();
            }
        }

        private class CachedMemberColumnLoader : ICachedColumnLoader
        {
            private readonly bool _needContains;
            private readonly MemberInfo[] _members;
            public CachedMemberColumnLoader(MemberInfo[] members, bool needContains)
            {
                _members = members;
                _needContains = needContains;
            }

            public ColumnInfo[] GetColumns(IEnumerable<ColumnInfo> columns)
            {
                return columns.Where(x => _members.Contains(x.column) == _needContains).ToArray();
            }
        }

        private class DefaultCachedColumnLoader : ICachedColumnLoader
        {
            public ColumnInfo[] GetColumns(IEnumerable<ColumnInfo> columns)
            {
                return columns.ToArray();
            }
        }

        private interface ICachedColumnLoader
        {
            ColumnInfo[] GetColumns(IEnumerable<ColumnInfo> columns);
        }
    }
}

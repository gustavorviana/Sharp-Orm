using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Interceptors
{
    /// <summary>
    /// Strongly-typed entity entry.
    /// </summary>
    public class DbEntry<T> : DbEntry
    {
        private readonly Dictionary<string, PropertyEntry> _properties = new Dictionary<string, PropertyEntry>();
        private readonly ObjectReader _reader;

        /// <summary>
        /// Gets all property entries for this entity.
        /// </summary>
        public IReadOnlyDictionary<string, PropertyEntry> Properties => _properties;

        /// <summary>
        /// The entity instance.
        /// </summary>
        public T Entity { get; }

        internal DbEntry(ObjectReader reader, TableInfo tableInfo, T entity, EntryState state) : base(tableInfo, state)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Entity = entity;

            InitializeProperties();
        }

        /// <summary>
        /// Gets a strongly-typed property entry.
        /// </summary>
        public PropertyEntry Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            var propertyName = GetPropertyName(propertyExpression);
            return Property(propertyName);
        }

        /// <summary>
        /// Reloads the entity properties from the current entity state.
        /// </summary>
        public override void Reload()
        {
            InitializeProperties();
        }

        /// <summary>
        /// Gets a property entry by name.
        /// </summary>
        public PropertyEntry Property(string propertyName)
        {
            _properties.TryGetValue(propertyName, out var property);
            return property;
        }

        /// <summary>
        /// Gets all modified properties.
        /// </summary>
        public IEnumerable<PropertyEntry> GetModifiedProperties()
        {
            return _properties.Values.Where(p => p.IsModified);
        }

        /// <summary>
        /// Applies current property values to the entity.
        /// </summary>
        public override void ApplyChanges()
        {
            if (State == EntryState.Remove || State == EntryState.None)
            {
                base.ApplyChanges();
                return;
            }

            foreach (var property in Properties.Values)
                if (property.IsModified)
                    property.PropertyInfo.SetValue(Entity, property.CurrentValue);

            _originalCells = _reader.ReadCells(Entity).ToArray();
            base.ApplyChanges();
        }

        private void InitializeProperties()
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in properties)
            {
                var currentValue = prop.GetValue(Entity);
                SetPropertyEntry(prop, currentValue, currentValue);
            }

            _originalCells = _reader.ReadCells(Entity).ToArray();
        }

        public override bool HasChanges()
        {
            return base.HasChanges() || _properties.Values.Any(p => p.IsModified); ;
        }

        /// <summary>
        /// Adds or updates a property entry.
        /// </summary>
        protected void SetPropertyEntry(PropertyInfo propertyInfo, object originalValue, object currentValue)
        {
            var entry = new PropertyEntry(propertyInfo, originalValue, currentValue);
            _properties[propertyInfo.Name] = entry;
        }

        private string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
                return memberExpression.Member.Name;

            throw new ArgumentException("Expression must be a property access", nameof(propertyExpression));
        }
    }

    /// <summary>
    /// Base class for entity entries.
    /// </summary>
    public class DbEntry
    {
        private readonly EntryState _originalState;

        protected Cell[] _originalCells;
        protected Cell[] _cells = DotnetUtils.EmptyArray<Cell>();

        /// <summary>
        /// The current state of the entity.
        /// </summary>
        public EntryState State { get; private set; }

        /// <summary>
        /// The name of the table this entity maps to.
        /// </summary>
        public string TableName => TableInfo.Name;

        /// <summary>
        /// The table information from Sharp ORM.
        /// </summary>
        public TableInfo TableInfo { get; }

        /// <summary>
        /// Gets the type of the entity.
        /// </summary>
        public virtual Type EntityType => TableInfo.Type;

        internal DbEntry(TableInfo tableInfo, EntryState originalState, Cell[] originalCells)
        {
            TableInfo = tableInfo ?? throw new ArgumentNullException(nameof(tableInfo));
            _originalState = originalState;
            _originalCells = originalCells;
        }

        internal DbEntry(TableInfo tableInfo, EntryState originalState)
        {
            TableInfo = tableInfo ?? throw new ArgumentNullException(nameof(tableInfo));
        }

        /// <summary>
        /// Checks if the entity has any modified properties.
        /// </summary>
        public virtual bool HasChanges()
        {
            return _originalState != State || _originalCells != _cells;
        }

        /// <summary>
        /// Reloads the entity properties from the current entity state.
        /// </summary>
        public virtual void Reload()
        {
            _cells = DotnetUtils.EmptyArray<Cell>();
            State = _originalState;
        }

        /// <summary>
        /// Applies current property values to the entity.
        /// </summary>
        public virtual void ApplyChanges()
        {
            _originalCells = _cells;
        }

        /// <summary>
        /// Sets the entity entry state to <see cref="EntryState.Add"/> and specifies which cells (columns) should be added.
        /// Throws an exception if the entity is in the <see cref="EntryState.Remove"/> state.
        /// </summary>
        /// <param name="cells">The cells representing the columns and values to add.</param>
        /// <exception cref="InvalidOperationException">Thrown if the entity is in the Remove state.</exception>
        /// <exception cref="ArgumentException">Thrown if no cells are provided for add.</exception>
        public void SetAddState(params Cell[] cells)
        {
            if (_originalState == EntryState.Remove)
                throw new InvalidOperationException("Cannot add an entity that is in the Remove state.");

            if (cells == null || cells.Length == 0)
                throw new ArgumentException("At least one cell must be provided for add.", nameof(cells));

            State = EntryState.Add;
            _cells = cells ?? DotnetUtils.EmptyArray<Cell>();
        }

        /// <summary>
        /// Sets the entity entry state to <see cref="EntryState.Remove"/>.
        /// Throws an exception if the entity is in the <see cref="EntryState.Add"/> state.
        /// </summary>
        public void SetRemoveState()
        {
            if (_originalState == EntryState.Add)
                throw new InvalidOperationException("Cannot remove an entity that is in the Add state.");

            State = EntryState.Remove;
            _cells = DotnetUtils.EmptyArray<Cell>();
        }

        /// <summary>
        /// Sets the entity entry state to <see cref="EntryState.Update"/> and specifies which cells (columns) should be updated.
        /// </summary>
        /// <param name="toUpdateCells">The cells representing the columns and values to update.</param>
        /// <exception cref="InvalidOperationException">Thrown if the entity is in the Add state.</exception>
        /// <exception cref="ArgumentException">Thrown if no cells are provided for update.</exception>
        public void SetUpdateState(params Cell[] toUpdateCells)
        {
            if (_originalState == EntryState.Add)
                throw new InvalidOperationException("Cannot update an entity that is in the Add state.");

            if (toUpdateCells == null || toUpdateCells.Length == 0)
                throw new ArgumentException("At least one cell must be provided for update.", nameof(toUpdateCells));

            State = EntryState.Update;
            _cells = toUpdateCells;
        }

        /// <summary>
        /// Sets the entity entry state to <see cref="EntryState.None"/> and clears any tracked cells.
        /// </summary>
        public void Ignore()
        {
            _cells = DotnetUtils.EmptyArray<Cell>();
            State = EntryState.None;
        }

        /// <summary>
        /// Gets the cells currently tracked for this entity entry.
        /// </summary>
        internal Cell[] GetCells()
        {
            return _cells;
        }
    }
}

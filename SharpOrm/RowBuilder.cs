using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SharpOrm
{
    /// <summary>
    /// Generic row builder for strongly-typed object construction with column expression support.
    /// Provides type-safe methods for building database rows with foreign key management.
    /// </summary>
    /// <typeparam name="TObject">The type of object this row builder is configured for</typeparam>
    public class RowBuilder<TObject> : RowBuilder, IFkNodeRoot, INodeCreationListener
    {
        private readonly QueryInfo _queryInfo;
        private readonly ForeignKeyRegister _fkRegister;

        /// <summary>
        /// Gets the foreign key register for managing foreign key relationships
        /// </summary>
        ForeignKeyRegister IFkNodeRoot.ForeignKeyRegister { get; }

        /// <summary>
        /// Gets the query information including configuration and table details
        /// </summary>
        QueryInfo IWithQueryInfo.Info => _queryInfo;

        private readonly ExpressionProcessor<TObject> _expression;

        /// <summary>
        /// Initializes a new instance of the RowBuilder for the specified object type
        /// </summary>
        /// <param name="config">The query configuration containing translation and table information</param>
        public RowBuilder(QueryConfig config)
        {
            var table = config.Translation.GetTable(typeof(TObject));
            _queryInfo = new QueryInfo(config, new DbName(table.Name, null));
            _fkRegister = new ForeignKeyRegister(table, _queryInfo.TableName, this);
            _expression = new ExpressionProcessor<TObject>(this, ExpressionConfig.SubMembers);
        }

        /// <summary>
        /// Adds a strongly-typed column value using a lambda expression to specify the column
        /// </summary>
        /// <typeparam name="TValue">The type of the column value</typeparam>
        /// <param name="column">Lambda expression identifying the column</param>
        /// <param name="value">The value to add for the specified column</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder<TObject> Add<TValue>(Expression<ColumnExpression<TObject, TValue>> column, TValue value)
        {
            return (RowBuilder<TObject>)Add(_expression.ParseColumn(column).Name, _queryInfo.Config.Translation.ToSql(value));
        }

        /// <summary>
        /// Handles foreign key node creation events - throws NotSupportedException as foreign keys are not supported
        /// </summary>
        /// <param name="node">The foreign key node that was created</param>
        /// <exception cref="NotSupportedException">Always thrown as foreign keys are not supported by RowBuilder</exception>
        void INodeCreationListener.Created(ForeignKeyNode node)
            => throw new NotSupportedException("Foreign keys are not supported by RowBuilder.");

        /// <summary>
        /// Resets the row builder, clearing all added values
        /// </summary>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public new RowBuilder<TObject> Reset() => (RowBuilder<TObject>)base.Reset();

        /// <summary>
        /// Adds all cells from another row with a specified prefix
        /// </summary>
        /// <param name="prefix">The prefix to add to each cell name</param>
        /// <param name="row">The row containing cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public new RowBuilder<TObject> Add(string prefix, Row row) => (RowBuilder<TObject>)base.Add(prefix, row);

        /// <summary>
        /// Adds all cells from another row
        /// </summary>
        /// <param name="row">The row containing cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public new RowBuilder<TObject> Add(Row row) => (RowBuilder<TObject>)base.Add(row);

        /// <summary>
        /// Adds multiple cells to the row builder
        /// </summary>
        /// <param name="cells">Array of cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public new RowBuilder<TObject> Add(params Cell[] cells) => (RowBuilder<TObject>)base.Add(cells);

        /// <summary>
        /// Adds multiple cells with a specified prefix to each cell name
        /// </summary>
        /// <param name="prefix">The prefix to add to each cell name</param>
        /// <param name="cells">Array of cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public new RowBuilder<TObject> Add(string prefix, params Cell[] cells) => (RowBuilder<TObject>)base.Add(prefix, cells);

        /// <summary>
        /// Adds a single key-value pair to the row builder
        /// </summary>
        /// <param name="key">The column name or key</param>
        /// <param name="value">The value to associate with the key</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public new RowBuilder<TObject> Add(string key, object value) => (RowBuilder<TObject>)base.Add(key, value);
    }

    /// <summary>
    /// Base row builder class for constructing database rows with key-value pairs.
    /// Provides fundamental operations for building and managing row data.
    /// </summary>
    public class RowBuilder
    {
        private readonly Dictionary<string, object> _cells = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the value for a specified key
        /// </summary>
        /// <param name="key">The key to get or set</param>
        /// <returns>The value associated with the specified key</returns>
        public object this[string key]
        {
            get => _cells[key];
            set => _cells[key] = value;
        }

        /// <summary>
        /// Clears all cells from the row builder
        /// </summary>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder Reset()
        {
            _cells.Clear();
            return this;
        }

        /// <summary>
        /// Adds all cells from another row with a specified prefix to each cell name
        /// </summary>
        /// <param name="prefix">The prefix to add to each cell name</param>
        /// <param name="row">The row containing cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder Add(string prefix, Row row)
        {
            return Add(prefix, row.Cells);
        }

        /// <summary>
        /// Adds all cells from another row
        /// </summary>
        /// <param name="row">The row containing cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder Add(Row row)
        {
            return Add(row.Cells);
        }

        /// <summary>
        /// Adds multiple cells to the row builder
        /// </summary>
        /// <param name="cells">Array of cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder Add(params Cell[] cells)
        {
            foreach (var cell in cells)
                Add(cell.Name, cell.Value);

            return this;
        }

        /// <summary>
        /// Adds multiple cells with a specified prefix to each cell name
        /// </summary>
        /// <param name="prefix">The prefix to add to each cell name</param>
        /// <param name="cells">Array of cells to add</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder Add(string prefix, params Cell[] cells)
        {
            foreach (var cell in cells)
                Add($"{prefix}{cell.Name}", cell.Value);

            return this;
        }

        /// <summary>
        /// Adds a single key-value pair to the row builder
        /// </summary>
        /// <param name="key">The column name or key</param>
        /// <param name="value">The value to associate with the key</param>
        /// <returns>The current RowBuilder instance for method chaining</returns>
        public RowBuilder Add(string key, object value)
        {
            _cells.Add(key, value);
            return this;
        }

        /// <summary>
        /// Creates a Row instance from the current state of the row builder
        /// </summary>
        /// <returns>A new Row containing all the cells from this builder</returns>
        public Row GetRow()
        {
            return new Row(GetCells());
        }

        /// <summary>
        /// Gets all cells as an array from the current row builder state
        /// </summary>
        /// <returns>Array of Cell objects representing all key-value pairs</returns>
        public Cell[] GetCells()
        {
            var cells = new Cell[_cells.Count];

            int index = 0;
            foreach (var rawCell in _cells)
                cells[index++] = new Cell(rawCell.Key, rawCell.Value);

            return cells;
        }

        /// <summary>
        /// Implicit conversion operator to convert RowBuilder to Row
        /// </summary>
        /// <param name="builder">The RowBuilder to convert</param>
        /// <returns>A Row instance created from the builder's current state</returns>
        public static implicit operator Row(RowBuilder builder)
        {
            return builder.GetRow();
        }
    }
}
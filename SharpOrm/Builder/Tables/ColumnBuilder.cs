using SharpOrm.Builder.Grammars.Table;
using System;
using System.Data;

namespace SharpOrm.Builder.Tables
{
    internal class ColumnBuilder : IColumnBuilder
    {
        private readonly ITableBuilder _table;
        internal readonly DataColumn _column;

        internal ColumnBuilder(ITableBuilder table, DataColumn column)
        {
            _table = table;
            _column = column;
        }

        public IColumnBuilder HasType(Type type)
        {
            _column.DataType = type;
            return this;
        }

        public IColumnBuilder IsRequired(bool isRequired = true)
        {
            _column.AllowDBNull = !isRequired;

            return this;
        }

        public IColumnBuilder IsOptional()
        {
            _column.AllowDBNull = true;

            return this;
        }

        public IColumnBuilder HasColumnType(string columnType)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.ColumnType] = columnType;

            return this;
        }

        public IColumnBuilder IsUnicode(bool isUnicode = true)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.IsUnicode] = isUnicode;

            return this;
        }

        public IColumnBuilder HasPrecision(int precision, int scale = 0)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.Precision] = precision;
            _column.ExtendedProperties[ExtendedPropertyKeys.Scale] = scale;

            return this;
        }

        public IColumnBuilder HasMaxLength(int maxLength)
        {
            _column.MaxLength = maxLength;
            _column.ExtendedProperties[ExtendedPropertyKeys.MaxLength] = maxLength;

            return this;
        }

        public IColumnBuilder HasDefaultValue(string defaultValue)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.DefaultValue] = defaultValue;

            return this;
        }

        public IColumnBuilder HasForeignTable(string foreignTable)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.ForeignTable] = foreignTable;

            return this;
        }

        public IColumnBuilder HasOnDelete(string onDelete)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.OnDelete] = onDelete;

            return this;
        }

        public IColumnBuilder HasOnUpdate(string onUpdate)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.OnUpdate] = onUpdate;

            return this;
        }

        public IColumnBuilder HasForeignKeyName(string foreignKeyName)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.ForeignKeyName] = foreignKeyName;

            return this;
        }

        public IColumnBuilder HasComputedExpression(string computedExpression)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.ComputedExpression] = computedExpression;
            _column.Expression = computedExpression;

            return this;
        }

        public IColumnBuilder IsVirtual(bool isVirtual = true)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.IsVirtual] = isVirtual;

            return this;
        }

        public IColumnBuilder HasIdentitySeed(int identitySeed)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.IdentitySeed] = identitySeed;
            _column.AutoIncrementSeed = identitySeed;

            return this;
        }

        public IColumnBuilder HasIdentityIncrement(int identityIncrement)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.IdentityIncrement] = identityIncrement;
            _column.AutoIncrementStep = identityIncrement;

            return this;
        }

        public IColumnBuilder IsIdentity(int seed = 1, int increment = 1)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.IdentitySeed] = seed;
            _column.ExtendedProperties[ExtendedPropertyKeys.IdentityIncrement] = increment;
            _column.AutoIncrement = true;
            _column.AutoIncrementSeed = seed;
            _column.AutoIncrementStep = increment;

            return this;
        }

        public IColumnBuilder HasComment(string comment)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.Comment] = comment;

            return this;
        }

        public IColumnBuilder HasDescription(string description)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.Description] = description;

            return this;
        }

        public IColumnBuilder HasCollation(string collation)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.Collation] = collation;

            return this;
        }

        public IColumnBuilder HasPosition(int position)
        {
            _column.ExtendedProperties[ExtendedPropertyKeys.Position] = position;

            return this;
        }

        public IColumnBuilder HasExtendedProperty(string key, object value)
        {
            _column.ExtendedProperties[key] = value;

            return this;
        }

        public IColumnBuilder HasForeignKey(string referencedTable, string referencedColumn = "Id", string constraintName = null)
        {
            _table.HasForeignKey(_column.ColumnName, referencedTable, referencedColumn, constraintName);
            return this;
        }
    }
}

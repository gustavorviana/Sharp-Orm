using System;
using System.Collections.Generic;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class ForeignInfo
    {
        private Dictionary<object, ColumnInfo> fkObjs = new Dictionary<object, ColumnInfo>();

        public object ForeignKey { get; }
        public string TableName { get; }
        public Type Type { get; }

        public ForeignInfo(LambdaColumn column, object foreignKey)
        {
            this.TableName = TableInfo.GetNameOf(column.ValueType);
            this.ForeignKey = foreignKey;
            this.Type = column.ValueType;
        }

        public void AddFkColumn(object owner, ColumnInfo column)
        {
            this.fkObjs.Add(owner, column);
        }

        public void SetForeignValue(object value)
        {
            foreach (var item in fkObjs)
                item.Value.SetRaw(item.Key, value);
        }

        public bool IsFk(Type type, object foreignKey)
        {
            return this.Type == type && this.ForeignKey.Equals(foreignKey);
        }
    }
}

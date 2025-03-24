using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader
{
    internal class ForeignInfo
    {
        private Dictionary<object, ColumnInfo> fkObjs = new Dictionary<object, ColumnInfo>();

        public object ForeignKey { get; }
        public string TableName { get; }
        public string LocalKey { get; }
        public Type Type { get; }

        public ForeignInfo(MemberInfo member, object foreignKey, string localKey)
        {
            Type = ReflectionUtils.GetMemberType(member);

            TableName = TranslationRegistry.Default.GetTableName(Type);
            ForeignKey = foreignKey;
            LocalKey = localKey;
        }

        public void AddFkColumn(object owner, ColumnInfo column)
        {
            fkObjs.Add(owner, column);
        }

        public void SetForeignValue(object value)
        {
            foreach (var item in fkObjs)
                item.Value.SetRaw(item.Key, value);
        }

        public bool IsFk(Type type, object foreignKey)
        {
            return Type == type && ForeignKey.Equals(foreignKey);
        }
    }
}

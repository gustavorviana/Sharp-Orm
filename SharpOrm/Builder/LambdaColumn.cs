using System;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal class LambdaColumn : Column, IEquatable<LambdaColumn>
    {
        public string PropertyName { get; }
        public Type DeclaringType { get; }
        public Type ValueType { get; }

        internal LambdaColumn(MemberInfo member)
        {
            this.DeclaringType = member.DeclaringType;

            this.Name = ColumnInfo.GetName(member);
            this.PropertyName = member.Name;
            this.ValueType = GetValueType(member);
        }

        private static Type GetValueType(MemberInfo member)
        {
            if (member is PropertyInfo prop)
                return prop.PropertyType;

            if (member is FieldInfo field)
                return field.FieldType;

            throw new NotSupportedException();
        }

        public bool Equals(LambdaColumn other)
        {
            return other != null &&
                this.PropertyName == other.PropertyName &&
                this.DeclaringType == other.DeclaringType &&
                this.ValueType == other.ValueType &&
                this.Name == other.Name;
        }

        public bool IsSame(ColumnInfo column)
        {
            return this.DeclaringType == column.DeclaringType && this.PropertyName == column.PropName;
        }
    }
}

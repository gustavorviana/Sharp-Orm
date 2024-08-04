﻿using System;
using System.Reflection;

namespace SharpOrm.Builder
{
    public class MemberInfoColumn : Column, IEquatable<MemberInfoColumn>
    {
        public string PropertyName { get; }
        public Type DeclaringType { get; }
        public Type ValueType { get; }

        internal MemberInfoColumn(MemberInfo member)
        {
            this.DeclaringType = member.DeclaringType;

            this.ValueType = ReflectionUtils.GetMemberValueType(member);
            this.Name = ColumnInfo.GetName(member);
            this.PropertyName = member.Name;
        }

        public bool Equals(MemberInfoColumn other)
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

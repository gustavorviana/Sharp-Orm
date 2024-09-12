using System;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    public class MemberInfoColumn : Column, IEquatable<MemberInfoColumn>
    {
        private readonly MemberInfo member;

        public string PropertyName => this.member.Name;
        public Type DeclaringType => member.DeclaringType;
        public Type ValueType { get; }

        internal MemberInfoColumn(MemberInfo member)
        {
            this.ValueType = ReflectionUtils.GetMemberValueType(member);
            this.Name = ColumnInfo.GetName(member);
            this.member = member;
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

        public T GetAttribute<T>() where T : Attribute
        {
            return this.member.GetCustomAttribute<T>();
        }

        public override SqlExpression ToExpression(IReadonlyQueryInfo info, bool alias)
        {
            StringBuilder builder = new StringBuilder();

            if (info is QueryInfo qi && qi.Joins.Count > 0)
                builder.Append(info.Config.ApplyNomenclature(info.TableName.TryGetAlias(info.Config))).Append('.');

            builder.Append(info.Config.ApplyNomenclature(this.Name));

            if (alias && !string.IsNullOrEmpty(this.Alias))
                builder.Append(" AS ").Append(info.Config.ApplyNomenclature(this.Alias));

            return (SqlExpression)builder;
        }
    }
}

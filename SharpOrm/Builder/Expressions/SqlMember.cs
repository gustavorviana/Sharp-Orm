﻿using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlMember : IEquatable<SqlMember>
    {
        internal SqlMemberInfo[] Childs { get; set; }
        internal bool IsNativeType { get; }
        private readonly Type declaringType;

        public MemberInfo Member { get; }
        public bool IsStatic { get; }
        public string Name => ColumnInfo.GetName(Member);
        public string Alias { get; }

        public SqlMember(Type declaringType, MemberInfo member, SqlMemberInfo[] childs, string alias)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));

            IsNativeType = member.MemberType == MemberTypes.Method || TranslationUtils.IsNative(ReflectionUtils.GetMemberType(member), false);

            this.declaringType = declaringType;
            Member = member;
            Childs = childs ?? DotnetUtils.EmptyArray<SqlMemberInfo>();
            IsStatic = false;
            Alias = GetAlias(Name, alias, childs, IsNativeType);
        }

        public SqlMember(SqlMemberInfo staticMember, string alias)
        {
            if (staticMember == null) throw new ArgumentNullException(nameof(staticMember));

            Member = staticMember.Member;
            Childs = new[] { staticMember };
            IsStatic = true;
            IsNativeType = true;
            Alias = !string.IsNullOrEmpty(alias) && alias != Name ? alias : null;
        }

        private string GetAlias(string name, string alias, SqlMemberInfo[] childs, bool isNative)
        {
            if (!isNative && childs.Length > 0)
                name = childs[0].Name;

            if (Member.Name == alias)
                alias = null;

            return !string.IsNullOrEmpty(alias) && alias != name ? alias : null;
        }

        public bool Equals(SqlMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Member.Equals(other.Member) &&
                   IsStatic == other.IsStatic &&
                   Name == other.Name &&
                   Alias == other.Alias &&
                   Childs.SequenceEqual(other.Childs);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SqlMember)obj);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Alias ?? this.Member.Name);

            foreach (var item in Childs)
                builder.Append('.').Append(item.ToString());

            return builder.ToString();
        }

        public override int GetHashCode()
        {
            int hashCode = -1627946248;
            hashCode = hashCode * -1521134295 + EqualityComparer<SqlMemberInfo[]>.Default.GetHashCode(Childs);
            hashCode = hashCode * -1521134295 + EqualityComparer<MemberInfo>.Default.GetHashCode(Member);
            hashCode = hashCode * -1521134295 + IsStatic.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Alias);
            return hashCode;
        }

        internal SqlPropertyInfo GetInfo()
        {
            if (Member.MemberType == MemberTypes.Property || Member.MemberType == MemberTypes.Field)
                return new SqlPropertyInfo(declaringType, Member);

            throw new NotSupportedException();
        }

        public static bool operator ==(SqlMember left, SqlMember right)
        {
            return EqualityComparer<SqlMember>.Default.Equals(left, right);
        }

        public static bool operator !=(SqlMember left, SqlMember right)
        {
            return !(left == right);
        }
    }
}

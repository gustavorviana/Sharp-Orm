using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlMember : IEquatable<SqlMember>
    {
        private readonly SqlMemberInfo _member;

        internal SqlMemberInfo[] Path { get; }
        internal SqlMemberInfo[] Childs { get; set; }
        internal bool IsNativeType => _member.IsNativeType;
        public Type DeclaringType => _member.DeclaringType;

        public MemberInfo Member => _member.Member;
        public bool IsStatic { get; }
        public string Name { get; }
        public string Alias { get; }

        internal SqlMember(SqlMemberInfo[] path, SqlMemberInfo member, SqlMemberInfo[] childs, string name, string alias)
        {
            _member = member ?? throw new ArgumentNullException(nameof(member));

            Name = name;
            Path = path ?? DotnetUtils.EmptyArray<SqlMemberInfo>();
            Childs = childs ?? DotnetUtils.EmptyArray<SqlMemberInfo>();
            Alias = GetAlias(Name, alias, childs, IsNativeType);
        }

        internal SqlMember(SqlMemberInfo staticMember, string alias)
        {
            _member = staticMember ?? throw new ArgumentNullException(nameof(staticMember));

            Name = ColumnInfo.GetName(Member);
            Path = DotnetUtils.EmptyArray<SqlMemberInfo>();
            Childs = new[] { staticMember };
            IsStatic = true;
            Alias = !string.IsNullOrEmpty(alias) && alias != Name ? alias : null;
        }

        private string GetAlias(string name, string alias, SqlMemberInfo[] childs, bool isNative)
        {
            if (!isNative && childs.Length > 0)
                name = childs[0].Name;

            if (Member.Name == alias)
                alias = null;

            return !string.IsNullOrEmpty(alias) || alias != name ? alias : null;
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
                return new SqlPropertyInfo(DeclaringType, Member);

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

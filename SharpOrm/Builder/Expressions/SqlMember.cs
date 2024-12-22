using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlMember : IEquatable<SqlMember>
    {
        private readonly SqlMemberInfo[] _childs;

        public MemberInfo Member { get; }
        public bool IsStatic { get; }
        public string Name { get; }
        public string Alias { get; }
        public bool HasChilds => _childs.Length > 0;

        public SqlMember(MemberInfo member, SqlMemberInfo[] childs, string alias)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));

            Member = member;
            _childs = childs ?? new SqlMemberInfo[0];
            IsStatic = false;
            Name = member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name;
            Alias = !string.IsNullOrEmpty(alias) && alias != Name ? alias : null;
        }

        public SqlMember(SqlMemberInfo staticMember, string alias)
        {
            if (staticMember == null) throw new ArgumentNullException(nameof(staticMember));

            Member = staticMember.Member;
            _childs = new[] { staticMember };
            IsStatic = true;
            Name = Member.Name;
            Alias = !string.IsNullOrEmpty(alias) && alias != Name ? alias : null;
        }

        public SqlMember(IEnumerable<SqlMember> members)
        {
            if (members == null) throw new ArgumentNullException(nameof(members));

            var membersList = members.ToList();
            if (!membersList.Any()) throw new ArgumentException("Members collection cannot be empty", nameof(members));

            _childs = membersList.SelectMany(m => m.GetChilds()).ToArray();
            Member = _childs.First().Member;
            Name = Member.Name;
        }

        public SqlMemberInfo[] GetChilds()
        {
            return _childs;
        }

        public bool Equals(SqlMember other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Member.Equals(other.Member) &&
                   IsStatic == other.IsStatic &&
                   Name == other.Name &&
                   Alias == other.Alias &&
                   _childs.SequenceEqual(other._childs);
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

            foreach (var item in GetChilds())
                builder.Append('.').Append(item.ToString());

            return builder.ToString();
        }

        public override int GetHashCode()
        {
            int hashCode = -1627946248;
            hashCode = hashCode * -1521134295 + EqualityComparer<SqlMemberInfo[]>.Default.GetHashCode(_childs);
            hashCode = hashCode * -1521134295 + EqualityComparer<MemberInfo>.Default.GetHashCode(Member);
            hashCode = hashCode * -1521134295 + IsStatic.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Alias);
            hashCode = hashCode * -1521134295 + HasChilds.GetHashCode();
            return hashCode;
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

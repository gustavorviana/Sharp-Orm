using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlMember
    {
        public bool IsStatic { get; }
        private readonly SqlMemberInfo[] childs;
        public MemberInfo Member { get; }
        public bool HasChilds => childs != null && childs.Length > 0;
        public string Name { get; }
        public string Alias { get; }

        public SqlMember(SqlMemberInfo staticMember, string alias) : this(staticMember.Member, new[] { staticMember }, true)
        {
            Alias = !string.IsNullOrEmpty(alias) && alias != Member.Name ? alias : null;
            Name = Member.Name;
        }

        public SqlMember(MemberInfo member, SqlMemberInfo[] childs, string alias) : this(member, childs, false)
        {
            Alias = !string.IsNullOrEmpty(alias) && alias != member.Name ? alias : null;
            Name = member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name;
        }

        private SqlMember(MemberInfo member, SqlMemberInfo[] childs, bool isStatic)
        {
            Member = member;
            IsStatic = isStatic;
            this.childs = childs;
        }

        /// <summary>
        /// Children of the property (whether they are other properties or functions).
        /// </summary>
        /// <returns></returns>
        public SqlMemberInfo[] GetChilds() => childs;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Alias ?? this.Member.Name);

            foreach (var item in GetChilds())
                builder.Append('.').Append(item.ToString());

            return builder.ToString();
        }
    }
}

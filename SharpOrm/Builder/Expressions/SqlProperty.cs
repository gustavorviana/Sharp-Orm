using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SharpOrm.Builder.Expressions
{
    public class SqlProperty
    {
        public bool IsStatic { get; }
        private readonly SqlMemberInfo[] childs;
        public MemberInfo Member { get; }
        public string Alias { get; }
        public string Name { get; }

        public bool HasChilds => childs != null && childs.Length > 0;

        public SqlProperty(SqlMemberInfo staticMember, string alias)
        {
            Member = staticMember.Member;
            IsStatic = true;

            Name = Member.Name;
            childs = new[] { staticMember };
            Alias = !string.IsNullOrEmpty(alias) && alias != Member.Name ? alias : null;
        }

        public SqlProperty(MemberInfo member, SqlMemberInfo[] childs, string alias)
        {
            Member = member;

            this.childs = childs;
            Name = member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name;
            Alias = !string.IsNullOrEmpty(alias) && alias != member.Name ? alias : null;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(this.Member.Name);

            for (int i = 0; i < childs.Length; i++)
                builder.Append('.').Append(childs[i].ToString());

            return builder.ToString();
        }

        /// <summary>
        /// Children of the property (whether they are other properties or functions).
        /// </summary>
        /// <returns></returns>
        public SqlMemberInfo[] GetChilds() => childs;
    }
}
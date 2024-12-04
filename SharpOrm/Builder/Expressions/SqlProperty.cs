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
        private readonly SqlMemberInfo[] childs;
        public MemberInfo Member { get; }
        public string Alias { get; }
        public string Name { get; }

        public SqlProperty(MemberInfo member, SqlMemberInfo[] childs, string alias)
        {
            this.Member = member;

            this.childs = childs;
            this.Name = member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name;
            this.Alias = !string.IsNullOrEmpty(alias) && alias != member.Name ? alias : null;
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
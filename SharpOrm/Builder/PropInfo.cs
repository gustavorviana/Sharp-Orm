using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SharpOrm.Builder
{
    public class PropInfo
    {
        private readonly List<MemberInfo> members;
        public string Name { get; }
        public string Alias { get; }

        public PropInfo(List<MemberInfo> members, string memberName)
        {
            var mainMember = members.First();
            this.members = members;

            this.Name = mainMember.GetCustomAttribute<ColumnAttribute>()?.Name ?? mainMember.Name;

            if (!string.IsNullOrEmpty(mainMember.Name))
                Alias = memberName != mainMember.Name ? memberName : null;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(members[0].Name);

            for (int i = 1; i < members.Count; i++)
            {
                builder.Append('.').Append(members[i].Name);
                if (members[i].MemberType == MemberTypes.Method)
                    builder.Append("()");
            }

            return builder.ToString();
        }

        public List<MemberInfo> GetMemberInfos() => members;
    }
}
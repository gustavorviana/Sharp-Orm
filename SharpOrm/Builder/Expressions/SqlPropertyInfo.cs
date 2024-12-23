using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlPropertyInfo : SqlMemberInfo
    {
        public SqlPropertyInfo(Type declaringType, MemberInfo member)
            : base(declaringType, member)
        {
            if (member.MemberType != MemberTypes.Property &&
                member.MemberType != MemberTypes.Field)
            {
                throw new ArgumentException(
                    string.Format("Member type must be Property or Field, but was {0}",
                        member.MemberType),
                    "member");
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

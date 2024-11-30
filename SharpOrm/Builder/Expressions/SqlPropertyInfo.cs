using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public class SqlPropertyInfo : SqlMemberInfo
    {
        public override Type ValueType => ReflectionUtils.GetMemberType(this.Member);

        public SqlPropertyInfo(MemberInfo member) : base(member)
        {
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

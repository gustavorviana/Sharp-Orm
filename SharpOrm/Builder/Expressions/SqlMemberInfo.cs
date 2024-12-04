using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.Expressions
{
    public abstract class SqlMemberInfo
    {
        public MemberInfo Member { get; }

        public MemberTypes MemberType => this.Member.MemberType;
        public Type DeclaringType => Member.DeclaringType;
        public string Name => Member.Name;

        public SqlMemberInfo(MemberInfo member)
        {
            this.Member = member;
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return this.Member.GetCustomAttribute<T>();
        }
    }
}

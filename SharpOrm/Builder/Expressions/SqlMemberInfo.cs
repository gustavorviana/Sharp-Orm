using System;
using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    public abstract class SqlMemberInfo
    {
        public MemberInfo Member { get; }
        public MemberTypes MemberType => this.Member.MemberType;
        public Type DeclaringType { get; }
        public virtual string Name => Member.Name;

        protected SqlMemberInfo(Type declaringType, MemberInfo member)
        {
            this.DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            this.Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return this.Member.GetCustomAttribute<T>();
        }
    }
}

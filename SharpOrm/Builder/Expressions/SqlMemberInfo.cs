using System;
using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    public abstract class SqlMemberInfo
    {
        public MemberInfo Member { get; }
        public MemberTypes MemberType => Member.MemberType;
        public Type DeclaringType { get; }
        public virtual string Name => Member.Name;

        protected SqlMemberInfo(Type declaringType, MemberInfo member)
        {
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            Member = member ?? throw new ArgumentNullException(nameof(member));
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return Member.GetCustomAttribute<T>();
        }
    }
}

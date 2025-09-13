using SharpOrm.DataTranslation;
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

        public virtual bool IsNativeType { get; }

        protected SqlMemberInfo(Type declaringType, MemberInfo member)
        {
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            Member = member ?? throw new ArgumentNullException(nameof(member));

            IsNativeType = IsNative();
        }

        private bool IsNative()
        {
            return Member.MemberType == MemberTypes.Method ||
                TranslationUtils.IsNative(GetMemberType(), false); ;
        }

        public T GetCustomAttribute<T>() where T : Attribute
        {
            return Member.GetCustomAttribute<T>();
        }

        public abstract Type GetMemberType();
    }
}

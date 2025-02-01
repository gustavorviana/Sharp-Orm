using SharpOrm.Builder.Expressions;
using SharpOrm.Errors;
using System;
using System.Reflection;

namespace SharpOrm.SqlMethods
{
    internal class ForeignMemberException : DatabaseException
    {
        public Type DeclaringType { get; }
        public MemberInfo Member { get; }

        public ForeignMemberException(MemberInfo member, string message) : base(message)
        {
            this.Member = member;
            this.DeclaringType = member.DeclaringType;
        }

        public ForeignMemberException(Type declaringType, MemberInfo member, string message) : base(message)
        {
            this.Member = member;
            this.DeclaringType = declaringType;
        }

        public static ForeignMemberException JoinNotFound(SqlMemberInfo member, string expectedTable)
        {
            string mType = member.MemberType == MemberTypes.Property ? "property" : "field";
            return new ForeignMemberException(
                member.DeclaringType, 
                member.Member, 
                $"It's not possible to load the '{member.Name}' {mType}, there are no joins for the '{expectedTable}' table or defined for the '{member.DeclaringType}' type."
            );
        }
    }
}

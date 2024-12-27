using SharpOrm.Errors;
using System;
using System.Reflection;

namespace SharpOrm.SqlMethods
{
    internal class ForeignMemberException : DatabaseException
    {
        public MemberInfo Member { get; }

        public ForeignMemberException(MemberInfo member, string message) : base(message)
        {
            this.Member = member;
        }

        public static ForeignMemberException IncompatibleType(MemberInfo member)
        {
            string mType = member.MemberType == MemberTypes.Property ? "property" : "field";

            return new ForeignMemberException(member, $"It's not possible to load the {mType} '{member.Name}' because its type is incompatible.");
        }
    }
}

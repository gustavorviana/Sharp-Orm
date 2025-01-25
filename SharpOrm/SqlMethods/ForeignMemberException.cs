using SharpOrm.Errors;
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

        public static ForeignMemberException JoinNotFound(MemberInfo member, string expectedTable)
        {
            string mType = member.MemberType == MemberTypes.Property ? "property" : "field";
            return new ForeignMemberException(member, $"It's not possible to load the '{member.Name}' {mType}, there are no joins for the '{expectedTable}' table or defined for the '{member.DeclaringType}' type."
);
        }
    }
}

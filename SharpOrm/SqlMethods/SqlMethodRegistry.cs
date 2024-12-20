using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.SqlMethods.Mapps;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.SqlMethods
{
    public class SqlMethodRegistry
    {
        private readonly Dictionary<CallerKey, SqlMemberCaller> callers = new Dictionary<CallerKey, SqlMemberCaller>();

        public SqlMethodRegistry()
        {
            var strType = typeof(string);

            Add(strType, nameof(String.ToLower), new SqlStringLower());
            Add(strType, nameof(String.ToUpper), new SqlStringUpper());
        }

        public SqlMethodRegistry Add(Type ownerType, string memberName, SqlMemberCaller caller)
        {
            callers[new CallerKey(ownerType, memberName)] = caller;
            return this;
        }

        public SqlExpression ApplyMember(IReadonlyQueryInfo info, SqlProperty property)
        {
            SqlExpression column = new SqlExpression(property.IsStatic ? "" : info.Config.ApplyNomenclature(property.Name));

            foreach (var member in property.GetChilds())
                column = this.ApplyCaller(info, column, member);

            return column;
        }

        private SqlExpression ApplyCaller(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            if (!(callers.TryGetValue(new CallerKey(member), out var caller)))
                throw new NotSupportedException(string.Format(Messages.Mapper.NotSupported, member.DeclaringType, member.Name));

            return caller.GetSqlExpression(info, expression, member);
        }

        private struct CallerKey : IEquatable<CallerKey>
        {
            public Type Type { get; }
            public string Name { get; }

            public CallerKey(SqlMemberInfo member) : this(member.DeclaringType, member.Name)
            {

            }

            public CallerKey(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public override bool Equals(object obj)
            {
                return obj is CallerKey other && Equals(other);
            }

            public bool Equals(CallerKey other)
            {
                return Type == other.Type && Name == other.Name;
            }

            public override int GetHashCode()
            {
                int hashCode = -1979447941;
                hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(Type);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }

            public static bool operator ==(CallerKey left, CallerKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CallerKey left, CallerKey right)
            {
                return !(left == right);
            }

            public override string ToString()
            {
                return $"{this.Type}.{this.Name}";
            }
        }
    }
}

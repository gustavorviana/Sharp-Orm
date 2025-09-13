using SharpOrm.Builder;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DbRunTest.Comparators
{
    internal class MemberInfoComparator : EqualityComparer<MemberInfo>
    {
        public static new MemberInfoComparator Default { get; }

        static MemberInfoComparator()
        {
            Default = new MemberInfoComparator();
        }

        private MemberInfoComparator()
        {

        }

        public override bool Equals(MemberInfo x, MemberInfo y)
        {
            if (x == null && y == null) return true;

            return x.GetType() == y.GetType() &&
                x.Name == y.Name &&
                AreRelatedTypes(x.DeclaringType, y.DeclaringType) &&
                ReflectionUtils.GetMemberType(x) == ReflectionUtils.GetMemberType(y);
        }

        private bool AreRelatedTypes(Type a, Type b)
        {
            if (a == null || b == null) return a == b;
            return a == b || a.IsAssignableFrom(b) || b.IsAssignableFrom(a);
        }

        public override int GetHashCode(MemberInfo obj)
        {
            int hashCode = 1997043432;
            hashCode = hashCode * -1521134295 + obj.GetType().GetHashCode();
            hashCode = hashCode * -1521134295 + obj.Name.GetHashCode();
            hashCode = hashCode * -1521134295 + ReflectionUtils.GetTopMostBaseType(obj.DeclaringType).GetHashCode();
            hashCode = hashCode * -1521134295 + ReflectionUtils.GetMemberType(obj).GetHashCode();
            return hashCode;
        }
    }
}

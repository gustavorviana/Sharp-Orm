using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    public class MemberPath
    {
        public SqlMemberInfo TargetMember { get; set; }
        public List<SqlMemberInfo> Childs { get; set; } = new List<SqlMemberInfo>();
        public List<SqlMemberInfo> Path { get; set; } = new List<SqlMemberInfo>();
        public Type RootType { get; private set; }

        public MemberPath()
        {
        }

        public MemberPath(Type rootType)
        {
            RootType = rootType;
        }

        public bool IsStaticProperty()
        {
            return ReflectionUtils.IsStatic(TargetMember.Member) && TargetMember.MemberType == MemberTypes.Property;
        }

        public MemberPath AddMembers(IEnumerable<SqlMemberInfo> members)
        {
            foreach (var member in members)
                AddMember(member);

            return this;
        }

        public MemberPath AddMember(MemberExpression expression)
        {
            return AddMember(new SqlPropertyInfo(expression.Expression?.Type ?? expression.Type, expression.Member));
        }

        public MemberPath AddMember(SqlMemberInfo member)
        {
            if (TargetMember != null)
            {
                Path.Insert(0, member);
            }
            else if (member.MemberType == MemberTypes.Method || TranslationUtils.IsNative(member.DeclaringType, true))
            {
                Childs.Insert(0, member);
            }
            else
            {
                TargetMember = member;
                if (RootType == null)
                    RootType = GetMemberType(TargetMember.Member, member.DeclaringType);
            }

            return this;
        }

        public MemberPath LoadRootMember(Type rootType)
        {
            if (TargetMember != null)
                return this;

            TargetMember = Childs[0];
            Childs.RemoveAt(0);

            RootType = GetMemberType(TargetMember.Member, rootType);
            return this;
        }

        public static Type GetMemberType(MemberInfo member, Type rootType)
        {
            return member.DeclaringType.IsAssignableFrom(rootType) ? rootType : member.DeclaringType;
        }
    }
}

using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    public class TableMap<T>
    {
        private readonly List<ReflectedField> memberInfos = new List<ReflectedField>();
        public TranslationRegistry Registry { get; set; } = TranslationRegistry.Default;

        public void Property(Expression<Func<T, object>> expression, string name)
        {
            memberInfos.Add(new ReflectedField(this.Registry, name, PropertyPathVisitor.GetPropertyPaths(expression)));
        }

        public IEnumerable<ReflectedField> GetPaths() => memberInfos;

        private class PropertyPathVisitor : ExpressionVisitor
        {
            private readonly List<MemberInfo> members = new List<MemberInfo>();

            protected override Expression VisitMember(MemberExpression node)
            {
                members.Insert(0, node.Member);

                return base.VisitMember(node);
            }

            public static List<MemberInfo> GetPropertyPaths<K>(Expression<Func<K, object>> expression)
            {
                var visitor = new PropertyPathVisitor();
                visitor.Visit(expression);
                return visitor.members;
            }
        }
    }
}

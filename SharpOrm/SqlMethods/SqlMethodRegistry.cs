using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;
using SharpOrm.Msg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static SharpOrm.Msg.Messages;

namespace SharpOrm.SqlMethods
{
    public class SqlMethodRegistry
    {
        private readonly List<SqlMemberCaller> callers = new List<SqlMemberCaller>();

        public SqlMethodRegistry Add(SqlMemberCaller caller)
        {
            callers.Add(caller);
            return this;
        }

        public SqlExpression ApplyMember(IReadonlyQueryInfo info, SqlMember property, IForeignKeyNode parent)
        {
            SqlExpression column = GetMemberNameExpression(info, property, parent ?? GetNodeRegister(info));

            foreach (var member in property.Childs)
                column = ApplyCaller(info, column, member);

            return new QueryBuilder(info).Add(column).ToExpression();
        }

        private SqlExpression GetMemberNameExpression(IReadonlyQueryInfo info, SqlMember property, IForeignKeyNode node)
        {
            if (property.IsNativeType)
                return GetNativeTypeExpression(info, property, node);

            return GetForeignMemberExpression(info, property);
        }

        private SqlExpression GetNativeTypeExpression(IReadonlyQueryInfo info, SqlMember member, IForeignKeyNode node)
        {
            if (member.IsStatic || member.Member.MemberType == System.Reflection.MemberTypes.Method)
                return new SqlExpression(string.Empty);

            return new DeferredMemberColumn(new DeferredMemberColumnLoader(member, info, node));
        }

        private static SqlExpression GetForeignMemberExpression(IReadonlyQueryInfo info, SqlMember property)
        {
            if (property.Childs.Length == 0)
                throw new ForeignMemberException(property.Member, "A property of the foreign class must be provided.");

            var member = property.Childs[0];
            property.Childs = property.Childs.Skip(1).ToArray();

            return new DeferredMemberColumn(new DeferredMemberColumnLoader(property, info, null));
        }

        private SqlExpression ApplyCaller(IReadonlyQueryInfo info, SqlExpression expression, SqlMemberInfo member)
        {
            var caller = callers.FirstOrDefault(x => x.CanWork(member));
            if (caller == null)
                throw new NotSupportedException(string.Format(Messages.Mapper.NotSupported, member.DeclaringType, member.Name));

            return caller.GetSqlExpression(info, expression, member);
        }

        private ForeignKeyRegister GetNodeRegister(IReadonlyQueryInfo info)
        {
            return ((info as QueryInfo)?.Parent as IFkNodeRoot)?.ForeignKeyRegister;
        }

        private class DeferredMemberColumnLoader : IDeferredMemberColumnLoader
        {
            private readonly SqlMember _member;
            private readonly IForeignKeyNode _node;

            public IReadonlyQueryInfo Info { get; }

            public string ColumnName { get; }

            public DeferredMemberColumnLoader(SqlMember member, IReadonlyQueryInfo info, IForeignKeyNode node)
            {
                _member = member;
                _node = node;
                Info = info;

                ColumnName = GetColumnName();
            }

            private string GetColumnName()
            {
                return Info
                    .Config
                    .Translation
                    .GetTable(_member.Member.DeclaringType)
                    ?.GetColumn(_member.Member)
                    ?.Name ?? ColumnInfo.GetName(_member.Member);
            }

            public IForeignKeyNode GetNode()
            {
                if (!(_node is ForeignKeyRegister register))
                    return _node;

                var node = _node as ForeignKeyNodeBase;
                foreach (var item in _member.Path)
                    node = node?.Get(item.Member) ?? throw GetException();

                if (_member.Path.Length > 0)
                    return node;

                if (node.TableInfo.Type == _member.DeclaringType)
                    return node;

                throw GetException();
            }

            private ForeignMemberException GetException()
            {
                var info = _member.GetInfo();
                var tableName = Info.Config.Translation.GetTableName(info.DeclaringType);
                return ForeignMemberException.JoinNotFound(info, tableName);
            }

            public string GetParentPrefix()
            {
                var name = GetNode().Name;
                return Info.Config.ApplyNomenclature(name.TryGetAlias());
            }

            public bool NeedPrefix()
            {
                return Info is QueryInfo queryInfo && queryInfo.Joins.Count > 0;
            }
        }
    }
}

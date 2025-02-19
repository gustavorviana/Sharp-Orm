using SharpOrm;
using SharpOrm.Builder.Grammars.Interfaces;
using System.Linq;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlDeleteGrammar : MysqlGrammarBase, IDeleteGrammar
    {
        public MysqlDeleteGrammar(Query query) : base(query)
        {
        }

        public virtual void Build()
        {
            ThrowOffsetNotSupported();

            Builder.Add("DELETE");
            if (Query.Info.Joins.Count > 0)
                Builder.Add(' ').Add(TryGetAlias(Query));

            Builder.Add(" FROM ").Add(Info.TableName.GetName(true, Info.Config));

            ApplyJoins();
            WriteWhere(true);

            if (CanWriteOrderby())
                ApplyOrderBy();

            AddLimit();
        }

        public virtual void BuildIncludingJoins(DbName[] joinNames)
        {
            ThrowOffsetNotSupported();

            Builder.Add("DELETE");
            ApplyDeleteJoins(joinNames);
            Builder.Add(" FROM ").Add(Info.TableName.GetName(true, Info.Config));

            ApplyJoins();
            WriteWhere(true);

            if (CanWriteOrderby())
                ApplyOrderBy();

            AddLimit();
        }

        private void ApplyDeleteJoins(DbName[] joinNames)
        {
            Builder.Add(' ').Add(TryGetAlias(Query));

            foreach (var join in joinNames)
                Builder.Add(", ").Add(TryGetAlias(join));
        }
    }
}

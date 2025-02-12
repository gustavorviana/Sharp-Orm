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
            ApplyDeleteJoins();
            Builder.Add(" FROM ").Add(Info.TableName.GetName(true, Info.Config));

            ApplyJoins();
            WriteWhere(true);

            if (CanWriteOrderby())
                ApplyOrderBy();

            AddLimit();
        }

        /// <summary>
        /// Applies the delete joins to the query.
        /// </summary>
        protected void ApplyDeleteJoins()
        {
            if (!CanApplyDeleteJoins())
                return;

            Builder
                .Add(' ')
                .Add(TryGetTableAlias(Query));

            if (!IsMultipleTablesDeleteWithJoin())
                return;

            foreach (var join in Info.Joins.Where(j => CanDeleteJoin(j.Info)))
                Builder.Add(", ").Add(TryGetTableAlias(join));
        }

        /// <summary>
        /// Determines whether delete joins can be applied.
        /// </summary>
        /// <returns>True if delete joins can be applied; otherwise, false.</returns>
        protected virtual bool CanApplyDeleteJoins()
        {
            return Info.Joins.Any();
        }
    }
}

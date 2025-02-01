using SharpOrm;
using System.Linq;

namespace SharpOrm.Builder.Grammars.Mysql
{
    internal class MysqlDeleteGrammar : MysqlGrammarBase
    {
        public MysqlDeleteGrammar(GrammarBase grammar) : base(grammar)
        {
        }

        public virtual void Build()
        {
            ThrowOffsetNotSupported();

            builder.Add("DELETE");
            ApplyDeleteJoins();
            builder.Add(" FROM ").Add(Info.TableName.GetName(true, Info.Config));

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

            builder
                .Add(' ')
                .Add(TryGetTableAlias(Query));

            if (!IsMultipleTablesDeleteWithJoin())
                return;

            foreach (var join in Info.Joins.Where(j => CanDeleteJoin(j.Info)))
                builder.Add(", ").Add(TryGetTableAlias(join));
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

namespace SharpOrm.Builder.Grammars.Interfaces
{
    public interface IDeleteGrammar : IGrammarBase
    {
        void Build();

        void BuildIncludingJoins(DbName[] joinNames);
    }
}

namespace SharpOrm.Builder.Grammars.Interfaces
{
    public interface ISelectGrammar : IGrammarBase
    {
        void BuildCount(Column column);
        void BuildSelect(bool configureWhereParams);
    }
}

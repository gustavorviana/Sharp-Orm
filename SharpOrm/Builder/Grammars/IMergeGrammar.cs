namespace SharpOrm.Builder.Grammars
{
    public interface IMergeGrammar
    {
        void Build(MergeQueryInfo target, MergeQueryInfo source, string[] whereColumns, string[] updateColumns, string[] insertColumns);
    }
}

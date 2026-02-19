namespace SharpOrm.Builder
{
    public interface IInsertIntoBuilder
    {
        IInsertIntoBuilder Add(string targetColumn);
        IInsertIntoBuilder Add(string targetColumn, object value);
        IInsertIntoBuilder Add(string targetColumn, string sourceColumn);
        void Execute();
    }
}
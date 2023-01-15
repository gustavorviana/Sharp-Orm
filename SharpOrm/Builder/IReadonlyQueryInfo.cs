namespace SharpOrm.Builder
{
    public interface IReadonlyQueryInfo
    {
        IQueryConfig Config { get; }
        string From { get; }
        string Alias { get; }
    }
}

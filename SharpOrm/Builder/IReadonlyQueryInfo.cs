namespace SharpOrm.Builder
{
    public interface IReadonlyQueryInfo
    {
        IQueryConfig Config { get; }
        DbName TableName { get; }
    }
}

namespace SharpOrm.Builder
{
    public interface IReadonlyQueryInfo
    {
        QueryConfig Config { get; }
        DbName TableName { get; }
    }
}

namespace SharpOrm.Builder.Tables
{
    public interface IModelMapperConfiguration<T>
    {
        void Configure(ModelMapper<T> tableMap);
    }
}

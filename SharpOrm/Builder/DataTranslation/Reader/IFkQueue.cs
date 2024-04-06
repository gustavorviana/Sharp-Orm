namespace SharpOrm.Builder.DataTranslation.Reader
{
    public interface IFkQueue
    {
        void EnqueueForeign(object owner, object fkValue, ColumnInfo column);
    }
}

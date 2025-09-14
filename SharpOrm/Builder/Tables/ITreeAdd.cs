using static SharpOrm.Builder.Tables.ColumnCollection;

namespace SharpOrm.Builder.Tables
{
    internal interface ITreeAdd<T>
    {
        ITreeAdd<T> Add(ColumnInfo column);
    }
}

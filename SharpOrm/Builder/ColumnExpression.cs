namespace SharpOrm.Builder
{
    public delegate object ColumnExpression<T>(T arg);
    public delegate R ColumnExpression<T, R>(T arg);
}

namespace SharpOrm.QueryTest.Interfaces
{
    public interface IDeleteBuilderTest
    {
        void Delete();
        void DeleteLimit();
        void DeleteOrder();
        void DeleteWhere();
        void DeleteWhereJoin();
        void DeleteJoins();
    }
}

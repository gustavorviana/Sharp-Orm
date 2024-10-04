namespace QueryTest.Interfaces
{
    public interface IUpdateBuilderTest
    {
        void UpdateNoColumns();
        void Update();
        void UpdateWhereJoin();
        void UpdateCaseValue();
        void UpdateByColumn();
        void UpdateWhere();
    }
}

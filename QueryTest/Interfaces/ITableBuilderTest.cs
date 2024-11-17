namespace QueryTest.Interfaces
{
    public interface ITableBuilderTest
    {
        void ExistsTableTest();
        void ExistsTempTableTest();
        void DropTableTest();
        void DropTempTableTest();
        void CreateBasedTable();
        void CreateBasedTempTable();
        void CreateTable();
        void CreateTableMultiplePk();
    }
}

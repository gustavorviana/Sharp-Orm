namespace QueryTest.Interfaces
{
    public interface IInsertBuilderTest
    {
        void Insert();
        void InsertWithoutId();
        void InsertWIthRaw();
        void InsertExtendedClass();
        void BulkInsert();
        void InsertByBasicSelect();
        void InsertByBasicWithColumnNamesSelect();
        void InsertByExpressionSelect();
        void InsertByExpressionWithColumnNamesSelect();
    }
}

namespace QueryTest.Interfaces;

public interface ICountBuilderTest
{
    void Count();
    void CountDistinctWithOrder();
    void CountOneColumnDistinct();
    void CountMultipleColumnsDistinct();
    void CountAllDistinct();
    void CountAllOfTableDistinct();
    void CountColumnWithOrderBy();
    void CountWhere();
    void CountOffset();
    void CountJoin();
    void CountWithOrderBy();
}

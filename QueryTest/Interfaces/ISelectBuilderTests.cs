namespace QueryTest.Interfaces
{
    public interface ISelectBuilderTests
    {
        // Métodos de Select
        void Select();
        void Select2();
        void SelectCase();
        void SelectCase2();
        void SelectColumnsName();
        void SelectColumn();
        void SelectRawColumn();
        void SelectLimit();
        void SelectOffsetLimit();
        void SelectDistinct();
        void SelectLimitWhere();
        void SelectIsolatedWhere();
        void SelectMultipleWhere();
        void SelectWhereOr();
        void SelectWhereColumnsEquals();
        void SelectWhereSqlExpression();
        void SelectWhereStartsWith();
        void SelectWhereContains();
        void SelectWhereEndsWith();
        void SelectWhereRawColumn();
        void SelectWhereRawValue();
        void SelectIsolatedWhereAndCommonWhere();
        void SelectWhereSubCallback();
        void SelectInnerJoin();
        void SelectLeftJoin();
        void SelectJoinWhere();
        void SelectGroupByColumnName();
        void SelectHavingColumn();
        void SelectGroupByColumnObj();
        void SelectOrderBy();
        void SelectOrderByAlias();
        void SelectWhereLikeIn();
        void SelectWhereNotLikeIn();
        void SelectHaving();

        void SelectOrderByLambdaTest();
        void SelectOrderByLambdaWithJoinTest();
        void SelectGroupByLambdaTest();
        void SelectGroupByLambdaWithJoinTest();
        void SelectWhereBool();
        void SelectWhereBetween();
        void SelectWhereNotBetween();
        void SelectWhereExistsExpression();
        void SelectWhereNotExistsExpression();
        void SelectWhereExistsQuery();
        void SelectWhereNotExistsQuery();
        void SelectWhereIn();
        void SelectWhereInEmpty();
        void SelectWhereNotIn();
        void SelectWhereInColumn();
        void SelectWhereInExpression();
        void SelectWhereNotInExpression();
        void SelectWhereInList();
        void SelectWhereNotInList();
        void SelectWhereNotInColumn();
        void SelectWhereNot();
        void SelectWhereNotNull();
        void SelectWhereNull();
        void SelectSqlExpression();
        void SelectNonDecimalSqlExpression();
        void SelectDecimalSqlExpression();

        //Count
        void Count();
        void CountJoin();
        void CountDistinct();
        void CountDistinctColumn();
        void CountWhere();
        void CountDistinct2();
        void CountDistinct3();
        void CountAllDistinct();
        void CountAllOfTableDistinct();
        void CountOffset();

        // Cases
        void ColumnCase();
        void CaseEmptyCase();
        void ColumnCaseExpression();

        // Others
        void FixColumnName();
    }
}
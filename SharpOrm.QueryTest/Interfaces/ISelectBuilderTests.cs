namespace SharpOrm.QueryTest.Interfaces
{
    public interface ISelectBuilderTests
    {
        // Count Operations
        void CountAllDistinct();
        void CountAllOfTableDistinct();
        void PaginateDistinctColumn();
        void CountSelect();
        void CountJoinSelect();
        void CountDistinctSelect();
        void CountWhereSelect();
        void CountDistinctSelect2();
        void CountDistinctSelect3();
        void CountSelectJoin();
        void CountOffset();
        void CountNewSelectJoin();

        // Distinct Operations
        void SelectWithDistinct();
        void SelectWithOffsetLimitDistinct();

        // Group By Operations
        void GroupByLambdaTest();
        void GroupByLambdaWithJoinTest();
        void SelectGroupByColumnName();
        void SelectGroupByPaginate();
        void SelectWhereGroupByPaginate();
        void SelectHaving();
        void SelectHavingColumn();
        void SelectGroupByColumnObj();

        // Join Operations
        void OrderByLambdaWithJoinTest();
        void SelectJoinWithWhere();
        void SelectInnerJoin();
        void SelectLeftJoin();
        void SelectJoin();
        void SelectGroupByPaginateInnerJoin();

        // Where Operations
        void WhereLambdaTest();
        void WhereLambdaWithJoinTest();
        void WhereBool();
        void WhereBetween();
        void WhereNotBetween();
        void WhereExistsExpression();
        void WhereNotExistsExpression();
        void WhereExistsQuery();
        void WhereNotExistsQuery();
        void WhereIn();
        void WhereInEmpty();
        void WhereNotIn();
        void WhereInColumn();
        void WhereInExpression();
        void WhereNotInExpression();
        void WhereInList();
        void WhereNotInList();
        void WhereNotInColumn();
        void SelectWhereNot();
        void SelectWhereNotNull();
        void SelectWhereNull();
        void SelectWhereIn();
        void SelectWhereInQuery();
        void SelectWhereCallbackQuery();
        void SelectMultipleWhere();
        void SelectWhereOr();
        void SelectWhereColumnsEquals();
        void SelectWhereSqlExpression();
        void SelectWhereStartsWith();
        void SelectWhereContains();
        void SelectWhereEndsWith();
        void SelectWhereRawColumn();
        void SelectWhereRawValue();
        void SelectWhereCallback();
        void SelectWhereSubCallback();
        void SelectWhereLikeIn();
        void SelectWhereNotLikeIn();

        // Order By Operations
        void OrderByLambdaTest();
        void SelectAndOrderBy();
        void SelectOrderBy();
        void SelectOrderByWithAlias();

        // Pagination / Offset Operations
        void SelectOffset();
        void SelectOffsetWhere();
        void NewSelectOffset();
        void NewSelectOffsetLimit();
        void SelectWithOffsetLimit();
        void SelectAndPaginate();
        void SelectWhereAndPaginate();
        void SelectLimitWhere();
        void SelectLimit();

        // Basic Select Operations
        void BasicSelect();
        void Basic2Select();
        void Select();
        void SelectColumnsName();
        void SelectColumn();
        void SelectRawColumn();
        void SelectCase();
        void SelectCase2();
        void SelectWithLimit();
        void SelectBasicWhere();
        void SelectExpression();
        void SelectExpressionOr();

        // Miscellaneous / Advanced Cases
        void ColumnCase();
        void CaseEmptyCase();
        void ColumnCaseExpression();
        void BasicSqlExpressionSelect();
        void NonDecimalSqlExpressionSelect();
        void DecimalSqlExpressionSelect();
        void DeleteWithNoLock();
        void SelectWithEscapeStrings();
    }
}
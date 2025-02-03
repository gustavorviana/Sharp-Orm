using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class MergeTests(ITestOutputHelper output) : DbGrammarTestBase(output, new SqlServerQueryConfig { UseOldPagination = true })
    {
        [Fact]
        public void MergeTest()
        {
            var expected = new SqlExpression("MERGE INTO [TargetTable] [Target] USING [SrcTable] [Source] ON [Source].[Id]=[Target].[Id] AND [Source].[Description]=[Target].[Description] WHEN MATCHED THEN UPDATE SET[Target].[Name]=[Source].[Name], [Target].[Description]=[Source].[Description] WHEN NOT MATCHED THEN INSERT ([Name], [Description], [Status]) VALUES ([Source].[Name], [Source].[Description], [Source].[Status]);");

            using var query = new Query("TargetTable");

            var result = query.GetGrammar().Merge(
                new DbName("SrcTable"), 
                ["Id", "Description"], 
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }
    }
}

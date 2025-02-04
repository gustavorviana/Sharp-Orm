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
            var expected = new SqlExpression("MERGE INTO [TargetTable] [Target] USING [SrcTable] [Source] ON [Source].[Id]=[Target].[Id] AND [Source].[Description]=[Target].[Description] WHEN MATCHED THEN UPDATE SET [Target].[Name]=[Source].[Name], [Target].[Description]=[Source].[Description] WHEN NOT MATCHED THEN INSERT ([Name], [Description], [Status]) VALUES ([Source].[Name], [Source].[Description], [Source].[Status]);");

            using var query = new Query("TargetTable");

            var result = query.GetGrammar().Merge(
                new DbName("SrcTable"),
                ["Id", "Description"],
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }

        [Fact]
        public void MergeWithAlias()
        {
            var expected = new SqlExpression("MERGE INTO [TargetTable] [Tgt] USING [SrcTable] [Src] ON [Src].[Id]=[Tgt].[Id] AND [Src].[Description]=[Tgt].[Description] WHEN MATCHED THEN UPDATE SET [Tgt].[Name]=[Src].[Name], [Tgt].[Description]=[Src].[Description] WHEN NOT MATCHED THEN INSERT ([Name], [Description], [Status]) VALUES ([Src].[Name], [Src].[Description], [Src].[Status]);");

            using var query = new Query("TargetTable Tgt");

            var result = query.GetGrammar().Merge(
                new DbName("SrcTable Src"),
                ["Id", "Description"],
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }
    }
}

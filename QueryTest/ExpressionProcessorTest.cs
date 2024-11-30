using BaseTest.Utils;
using SharpOrm.Builder;
using System.Reflection;
using Xunit.Abstractions;

namespace QueryTest
{
    public class ExpressionProcessorTest(ITestOutputHelper? output) : TestBase(output)
    {
        [Fact]
        public void NewExpression()
        {
            var multipleExpression = ExpressionProcessor.ParseNewExpression<SampleClass>(x => new
            {
                x.Name,
                DateAlias = x.Date.Day.ToString()
            });

            Assert.Equal(2, multipleExpression.Length);

            Assert.Null(multipleExpression[0].Alias);
            Assert.Equal("Name", multipleExpression[0].ToString());
            Assert.Single(multipleExpression[0].GetMemberInfos());

            Assert.Equal("DateAlias", multipleExpression[1].Alias);
            Assert.Equal("Date.Day.ToString()", multipleExpression[1].ToString());
            Assert.Equal(3, multipleExpression[1].GetMemberInfos().Count);
        }

        [Fact]
        public void Expression()
        {
            var singleExpression = ExpressionProcessor.ParseExpression<SampleClass>(x => x.Date.Day.ToString().Trim());
            var members = singleExpression.GetMemberInfos();

            Assert.NotNull(singleExpression);
            Assert.Equal("Date.Day.ToString().Trim()", singleExpression.ToString());

            Assert.Equal(4, members.Count);
            Assert.Equal(MemberTypes.Property, members[0].MemberType);
            Assert.Equal(nameof(SampleClass.Date), members[0].Name);

            Assert.Equal(MemberTypes.Property, members[1].MemberType);
            Assert.Equal(nameof(DateTime.Day), members[1].Name);

            Assert.Equal(MemberTypes.Method, members[2].MemberType);
            Assert.Equal(nameof(ToString), members[2].Name);

            Assert.Equal(MemberTypes.Method, members[3].MemberType);
            Assert.Equal(nameof(string.Trim), members[3].Name);
        }

        public class SampleClass
        {
            public string? Name { get; set; }
            public DateTime Date { get; set; }
        }
    }
}

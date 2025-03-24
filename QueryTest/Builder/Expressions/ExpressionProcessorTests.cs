using BaseTest.Utils;
using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System.Reflection;

namespace QueryTest.Builder.Expressions
{
    public class ExpressionProcessorTests() : SqlMethodMapTest(new SqlServerQueryConfig())
    {
        private static readonly string StaticString = "Static Value";
        private readonly string InstanceString = "Instance String";
        private const string ConstString = "Const String";

        [Fact]
        public void NewExpressionWithNoChildsTest()
        {
            var processor = GetProcessor<SampleClass>(ExpressionConfig.New);
            var multipleExpression = processor.ParseExpression(x => new { x.Name }).ToArray();

            Assert.Throws<NotSupportedException>(() => processor.ParseExpression(x => new
            {
                x.Name,
                DateAlias = x.Date.Day.ToString()
            }).ToArray());

            Assert.Single(multipleExpression);
            Assert.Equal("Name", multipleExpression[0].Member.Name);
        }

        [Fact]
        public void NewExpressionTest()
        {
            var multipleExpression = GetProcessor<SampleClass>().ParseExpression(x => new
            {
                x.Name,
                DateAlias = x.Date.Day.ToString()
            }).ToArray();

            Assert.Equal(2, multipleExpression.Length);

            Assert.Equal("Name", multipleExpression[0].Member.Name);
            Assert.Equal("DateAlias", multipleExpression[1].Alias);
            Assert.Equal("DateAlias.Day.ToString()", multipleExpression[1].ToString());
            Assert.Equal(2, multipleExpression[1].Childs.Length);
        }

        [Fact]
        public void ExpressionWithNoChildsTest()
        {
            var processor = GetProcessor<SampleClass>(ExpressionConfig.New);
            var singleExpression = processor.ParseExpression(x => x.Date).First();

            Assert.Throws<NotSupportedException>(() => processor.ParseExpression(x => x.Date.Day.ToString().Trim()).First());
            Assert.Equal("Date", singleExpression.ToString());
        }

        [Fact]
        public void ExpressionTest()
        {
            var singleExpression = GetProcessor<SampleClass>().ParseExpression(x => x.Date.Day.ToString().Trim()).First();
            var members = singleExpression.Childs;

            Assert.Equal("Date.Day.ToString().Trim()", singleExpression.ToString());

            Assert.Equal(3, members.Length);
            Assert.Equal(nameof(SampleClass.Date), singleExpression.Member.Name);

            Assert.Equal(MemberTypes.Property, members[0].MemberType);
            Assert.Equal(nameof(DateTime.Day), members[0].Name);

            Assert.Equal(MemberTypes.Method, members[1].MemberType);
            Assert.Equal(nameof(ToString), members[1].Name);

            Assert.Equal(MemberTypes.Method, members[2].MemberType);
            Assert.Equal(nameof(string.Trim), members[2].Name);
        }

        [Fact]
        public void MethodDisabledTest()
        {
            var processor = GetProcessor<SampleClass>(ExpressionConfig.SubMembers);

            Assert.Throws<NotSupportedException>(() => processor.ParseExpression(x => x.Name!.Trim()).First());
        }

        [Fact]
        public void NoSubMembersTest()
        {
            var processor = GetProcessor<SampleClass>(ExpressionConfig.None);

            Assert.Throws<NotSupportedException>(() => processor.ParseExpression(x => x.Date.Day).First());
        }

        [Fact]
        public void ExpressionWithMethodLocalArgsTest()
        {
            var value = "Test";
            var singleExpression = GetProcessor<SampleClass>().ParseExpression(x => x.Name!.Insert(0, value)).First();
            var members = singleExpression.Childs;

            Assert.Equal("Name.Insert(Int32, String)", singleExpression.ToString());
            Assert.Single(members);

            Assert.Equal(MemberTypes.Property, singleExpression.Member.MemberType);
            Assert.Equal(nameof(SampleClass.Name), singleExpression.Member.Name);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(nameof(string.Insert), method.Name);

            Assert.Equal(2, method.Args.Length);
            Assert.Equal(0, method.Args[0]);
            Assert.Equal(value, method.Args[1]);
        }

        [Fact]
        public void ExpressionWithMethodStaticArgsTest()
        {
            var singleExpression = GetProcessor<SampleClass>().ParseExpression(x => x.Name!.Insert(0, StaticString)).First();
            var members = singleExpression.Childs;

            Assert.Single(members);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(StaticString, method.Args[1]);
        }

        [Fact]
        public void ExpressionWithMethodConstArgsTest()
        {
            var singleExpression = GetProcessor<SampleClass>().ParseExpression(x => x.Name!.Insert(0, ConstString)).First();
            var members = singleExpression.Childs;

            Assert.Single(members);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(ConstString, method.Args[1]);
        }

        [Fact]
        public void ExpressionWithMethodInstanceArgsTest()
        {
            var singleExpression = GetProcessor<SampleClass>().ParseExpression(x => x.Name!.Insert(0, InstanceString)).First();
            var members = singleExpression.Childs;

            Assert.Single(members);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(InstanceString, method.Args[1]);
        }

        public class SampleClass
        {
            public string? Name { get; set; }
            public DateTime Date { get; set; }

            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
        }
    }
}
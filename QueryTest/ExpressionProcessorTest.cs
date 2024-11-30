using BaseTest.Utils;
using SharpOrm.Builder.Expressions;
using System.Reflection;
using Xunit.Abstractions;

namespace QueryTest
{
    public class ExpressionProcessorTest(ITestOutputHelper? output) : TestBase(output)
    {
        private static readonly string StaticString = "Static Value";
        private readonly string InstanceString = "Instance String";
        private const string ConstString = "Const String";

        [Fact]
        public void NewExpressionWithNoChildsTest()
        {
            var processor = new ExpressionProcessor(false);
            var multipleExpression = processor.ParseNewExpression<SampleClass>(x => new { x.Name }).ToArray();

            Assert.Throws<NotSupportedException>(() => processor.ParseNewExpression<SampleClass>(x => new
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
            var multipleExpression = new ExpressionProcessor(true).ParseNewExpression<SampleClass>(x => new
            {
                x.Name,
                DateAlias = x.Date.Day.ToString()
            }).ToArray();

            Assert.Equal(2, multipleExpression.Length);

            Assert.Equal("Name", multipleExpression[0].Member.Name);
            Assert.Equal("DateAlias", multipleExpression[1].Alias);
            Assert.Equal("Date.Day.ToString()", multipleExpression[1].ToString());
            Assert.Equal(2, multipleExpression[1].GetChilds().Length);
        }

        [Fact]
        public void ExpressionWithNoChildsTest()
        {
            var processor = new ExpressionProcessor(false);
            var singleExpression = processor.ParseExpression<SampleClass>(x => x.Date);

            Assert.Throws<NotSupportedException>(() => processor.ParseExpression<SampleClass>(x => x.Date.Day.ToString().Trim()));
            Assert.Equal("Date", singleExpression.ToString());
        }

        [Fact]
        public void ExpressionTest()
        {
            var singleExpression = new ExpressionProcessor(true).ParseExpression<SampleClass>(x => x.Date.Day.ToString().Trim());
            var members = singleExpression.GetChilds();

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
        public void ExpressionWithMethodLocalArgsTest()
        {
            var value = "Test";
            var singleExpression = new ExpressionProcessor(true).ParseExpression<SampleClass>(x => x.Name!.Insert(0, value));
            var members = singleExpression.GetChilds();

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
            var singleExpression = new ExpressionProcessor(true).ParseExpression<SampleClass>(x => x.Name!.Insert(0, StaticString));
            var members = singleExpression.GetChilds();

            Assert.Single(members);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(StaticString, method.Args[1]);
        }

        [Fact]
        public void ExpressionWithMethodConstArgsTest()
        {
            var singleExpression = new ExpressionProcessor(true).ParseExpression<SampleClass>(x => x.Name!.Insert(0, ConstString));
            var members = singleExpression.GetChilds();

            Assert.Single(members);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(ConstString, method.Args[1]);
        }

        [Fact]
        public void ExpressionWithMethodInstanceArgsTest()
        {
            var singleExpression = new ExpressionProcessor(true).ParseExpression<SampleClass>(x => x.Name!.Insert(0, InstanceString));
            var members = singleExpression.GetChilds();

            Assert.Single(members);

            var method = Assert.IsType<SqlMethodInfo>(members[0]);
            Assert.Equal(InstanceString, method.Args[1]);
        }

        public class SampleClass
        {
            public string? Name { get; set; }
            public DateTime Date { get; set; }
        }
    }
}
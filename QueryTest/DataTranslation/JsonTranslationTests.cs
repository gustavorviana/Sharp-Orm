using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm.DataTranslation;
using Xunit.Abstractions;

namespace QueryTest.DataTranslation
{
    public class JsonTranslationTests(ITestOutputHelper? output) : DbMockTest(output)
    {
        [Fact]
        public void SerializeTest()
        {
            var registry = new TranslationRegistry();
            var data = new People { Name = "Test", Age = 10 };

            string json = Assert.IsType<string>(registry.JsonTranslation.ToSqlValue(data, null));

            Assert.Equal("{\"Name\":\"Test\",\"Age\":10}", json);
        }

        [Fact]
        public void DeserializeTest()
        {
            var registry = new TranslationRegistry();
            var json = "{\"Name\":\"Test\",\"Age\":10}";
            var data = Assert.IsType<People>(registry.JsonTranslation.FromSqlValue(json, typeof(People)));

            Assert.Equal("Test", data.Name);
            Assert.Equal(10, data.Age);
        }
    }
}

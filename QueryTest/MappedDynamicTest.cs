using BaseTest.Utils;
using Bogus;
using SharpOrm;
using SharpOrm.DataTranslation.Reader;
using System.Dynamic;

namespace QueryTest
{
    public class MappedDynamicTest : DbMockTest
    {
        [Fact]
        public void ReadTest()
        {
            // Arrange
            var faker = new Faker();
            var name = faker.Name.FirstName();
            var reader = GetReader(new Cell("id", 1), new Cell("Name", name), new Cell("random", DBNull.Value));

            var mapper = new MappedDynamic(reader);

            // Act
            dynamic result = mapper.Read(reader);

            // Assert
            Assert.IsType<ExpandoObject>(result);
            Assert.Equal(1, result.id);
            Assert.Equal(name, result.Name);
            Assert.Null(result.random);
        }

        [Fact]
        public void WriteTest()
        {
            // Arrange
            var faker = new Faker();
            string name = faker.Name.FirstName();
            int id = faker.Random.Number(0, 10);
            var table = Translation.GetTable(typeof(ExpandoObject));

            var obj = (IDictionary<string, object?>)new ExpandoObject();
            obj["id"] = id;
            obj["Name"] = name;

            // Act
            var cells = table.GetObjCells(obj, true, true).ToArray();

            // Assert
            Assert.Equal(2, cells.Length);
            Assert.Equal(name, cells.First(x => x.Name == "Name").Value);
            Assert.Equal(id, cells.First(x => x.Name == "id").Value);
        }
    }
}

using BaseTest.Mock;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation.Reader;
using System.Collections;
using System.Collections.ObjectModel;
using static QueryTest.ObjectActivatorTest;

namespace QueryTest
{
    public class MapperTest : DbMockTest
    {
        [Fact]
        public void InvalidList()
        {
            var reader = new MockDataReader(new Cell("Id", 1));
            var mapper = MappedObject.Create(reader, typeof(WithInvalidList));
            mapper.Read(reader);
        }

        [Theory]
        [InlineData(typeof(Collection<int>), typeof(Collection<>))]
        [InlineData(typeof(List<int>), typeof(List<>))]
        [InlineData(typeof(IList<int>), typeof(List<>))]
        [InlineData(typeof(ICollection<int>), typeof(List<>))]
        [InlineData(typeof(IReadOnlyCollection<int>), typeof(List<>))]
        [InlineData(typeof(IReadOnlyList<int>), typeof(List<>))]
        public void CreateList(Type type, Type expected)
        {
            var list = new RuntimeList(type);
            list.items.Add(1);
            list.items.Add(2);
            list.items.Add(3);

            Assert.True(RuntimeList.IsAnyGenericType(list.items.GetType(), expected));
        }

        [Theory]
        [InlineData(typeof(Collection<int>), typeof(Collection<int>))]
        [InlineData(typeof(Collection<int>), typeof(List<int>))]
        [InlineData(typeof(List<int>), typeof(List<int>))]
        [InlineData(typeof(List<int>), typeof(Collection<int>))]
        public void ToCollectionClass(Type listType, Type targetType)
        {
            var list = new RuntimeList(listType);
            list.items.Add(1);
            list.items.Add(2);
            list.items.Add(3);

            var result = list.ToCollection(targetType);
            Assert.IsType(targetType, result);
            Assert.Equal(3, ((IList)result).Count);
        }

        [Theory]
        [InlineData(typeof(IList<int>), typeof(List<int>))]
        [InlineData(typeof(ICollection<int>), typeof(List<int>))]
        [InlineData(typeof(IReadOnlyCollection<int>), typeof(List<int>))]
        [InlineData(typeof(IReadOnlyList<int>), typeof(Collection<int>))]
        public void ToCollectionByInterface(Type listType, Type targetType)
        {
            var list = new RuntimeList(listType);
            list.items.Add(1);
            list.items.Add(2);
            list.items.Add(3);

            var result = list.ToCollection(targetType);
            Assert.IsType(targetType, result);
            Assert.Equal(3, ((IList)result).Count);
        }

        private class WithInvalidList
        {
            public int Id { get; set; }
            public List<Level3> Level { get; set; } = null!;
        }
    }
}

using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueryTest.DataTranslation
{
    public class CustomSqlTranslationTests : DbMockTest
    {
        [Fact]
        public void GetTranslationTest()
        {
            TableInfo table = Translation.GetTable(typeof(CustomClassInfo));
            var owner = new CustomClassInfo();
            var cell = table.GetObjCells(owner, true, false).FirstOrDefault();
            Assert.Equal(2, cell?.Value);
        }

        [Fact]
        public void RecursiveCallTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [Recursive]", () => GetReader(new Cell("Id", 1)));

            using var query = new Query<RecursiveClass>(Manager);
            Assert.NotNull(query.FirstOrDefault());
        }

        [Fact]
        public void CustomIdTranslationTest()
        {
            var owner = new CustomIdClass { Id = new CustomId { Id = 10 } };
            TableInfo table = Translation.GetTable(typeof(CustomIdClass));

            var cell = table.GetObjCells(owner, true, false).FirstOrDefault();
            Assert.Equal(10, cell?.Value);
        }

        #region Classes
        private class CustomClassInfo
        {
            [SqlConverter(typeof(TestGetTranslator))]
            public int MyValue { get; set; }
        }

        [Table("Recursive")]
        private class RecursiveClass
        {
            public int Id { get; set; }

            [SqlConverter(typeof(CustomTranslation))]
            [Column("Child1_Id")]
            public RecursiveClass? Parent { get; set; }
        }

        public class TestGetTranslator : ISqlTranslation
        {
            public bool CanWork(Type type) => type == typeof(int) || type == typeof(RecursiveClass);

            public object FromSqlValue(object value, Type expectedType)
            {
                return 1;
            }

            public object ToSqlValue(object value, Type type)
            {
                return 2;
            }
        }

        internal class CustomTranslation : ISqlTranslation
        {
            public bool CanWork(Type type) => type == typeof(int);

            public object FromSqlValue(object value, Type expectedType)
            {
                return new RecursiveClass { };
            }

            public object ToSqlValue(object value, Type type)
            {
                return value;
            }
        }

        internal class CustomIdTranslation : ISqlTranslation
        {
            public bool CanWork(Type type) => type == typeof(int) || type == typeof(CustomId);

            public object FromSqlValue(object value, Type expectedType)
            {
                if (value is not int intValue)
                    throw new InvalidDataException("Value is not an integer");

                return new CustomId { Id = intValue };
            }

            public object ToSqlValue(object value, Type type)
            {
                if (value is int intValue)
                    return intValue;

                if (value is CustomId customId)
                    return customId.Id;

                throw new InvalidDataException("Value is not an integer or CustomId");
            }
        }

        private class CustomIdClass
        {
            [Column("Id")]
            public CustomId Id { get; set; }
        }

        [SqlConverter(typeof(CustomIdTranslation))]
        private struct CustomId
        {
            public int Id { get; set; }
        }
        #endregion
    }
}

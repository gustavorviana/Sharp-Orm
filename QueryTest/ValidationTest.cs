using SharpOrm.Builder;
using System.ComponentModel.DataAnnotations;

namespace QueryTest
{
    public class ValidationTest
    {
        [Fact]
        public void RequiredTest()
        {
            Validate(new ValidableClass { Id = 1 });
            Assert.Throws<ValidationException>(() => Validate(new ValidableClass()));
        }

        [Fact]
        public void RangeTest()
        {
            Assert.Throws<ValidationException>(() => Validate(new ValidableClass { Id = 11 }));
        }

        private static void Validate<T>(T value)
        {
            new TableInfo(typeof(T)).Validate(value);
        }

        private class ValidableClass
        {
            [Required]
            [Range(1, 10)]
            public int? Id { get; set; }
        }
    }
}

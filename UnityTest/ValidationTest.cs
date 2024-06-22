using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using System.ComponentModel.DataAnnotations;

namespace UnityTest
{
    [TestClass]
    public class ValidationTest
    {
        [TestMethod]
        public void RequiredTest()
        {
            Validate(new ValidableClass { Id = 1 });
            Assert.ThrowsException<ValidationException>(() => Validate(new ValidableClass()));
        }

        [TestMethod]
        public void RangeTest()
        {
            Assert.ThrowsException<ValidationException>(() => Validate(new ValidableClass { Id = 11 }));
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

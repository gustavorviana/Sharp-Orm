using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static SharpOrm.Builder.DataTranslation.ObjectTranslator;

namespace UnityTest
{
    [TestClass]
    public class ObjectTranslatorTest
    {
        private static readonly ObjectLoader loader = new(typeof(TestClass));

        [TestMethod]
        public void GetEnumValue()
        {
            AssertPropertyValue(1, new() { MyEnum = TestEnum.Val1 }, nameof(TestClass.MyEnum));
        }

        private static void AssertPropertyValue(object expected, TestClass objOwner, string propName)
        {
            var prop = loader.Properties[propName];

            Assert.IsNotNull(prop);
            Assert.AreEqual(expected, loader.GetColumnValue(GetColumnName(prop), objOwner, prop));
        }

        private class TestClass
        {
            public int MyId { get; set; }
            public string MyName { get; set; }
            public DateTime MyDate { get; set; }
            public TimeSpan MyTime { get; set; }
            public byte MyByte { get; set; }
            public TestEnum MyEnum { get; set; }
        }

        private enum TestEnum
        {
            Val1 = 1,
            Val2 = 2,
            Val3 = 3
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnityTest.Utils
{
    internal class TestAssert
    {
        public static void AreEqualsDate(DateTime expected, object actual, string message)
        {
            Assert.IsInstanceOfType(actual, typeof(DateTime));
            AreEqualsDate(expected, (DateTime)actual, message);
        }

        public static void AreEqualsDate(DateTime expected, DateTime actual, string message)
        {
            Assert.AreEqual(expected.Date, actual.Date);
            try
            {
                Assert.AreEqual(decimal.Truncate((decimal)expected.TimeOfDay.TotalSeconds), decimal.Truncate((decimal)actual.TimeOfDay.TotalSeconds), message);
            }
            catch
            {
                Console.WriteLine(expected);
                Console.WriteLine(actual);
                throw;
            }
        }
    }
}

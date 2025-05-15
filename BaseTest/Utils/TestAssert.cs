using SharpOrm.DataTranslation;
using Xunit;

namespace BaseTest.Utils
{
    public static class TestAssert
    {
        public static void EqualDate(DateTime expected, object actual, string message)
        {
            Assert.IsType<DateTime>(actual);
            EqualDate(expected, (DateTime)actual, message, TranslationRegistry.Default);
        }

        public static void EqualTime(TimeSpan expected, TimeSpan actual, string message)
        {
            var expectedSeconds = Math.Round(expected.TotalSeconds);
            var actualSeconds = Math.Round(actual.TotalSeconds);

            Assert.True(expectedSeconds == Math.Round(actual.TotalSeconds), message + $"({expectedSeconds}, {actualSeconds}); ({expected.TotalSeconds}; {actual.TotalSeconds})");
        }

        public static void EqualDate(DateTime expected, DateTime actual, string message, TranslationRegistry? registry = null)
        {
            registry ??= TranslationRegistry.Default;

            Assert.Equal(expected.Date.ToString(registry.DateFormat), actual.Date.ToString(registry.DateFormat));
        }
    }
}

﻿using Xunit;

namespace BaseTest.Utils
{
    public static class TestAssert
    {
        public static void EqualDate(DateTime expected, object actual, string message)
        {
            Assert.IsType<DateTime>(actual);
            EqualDate(expected, (DateTime)actual, message);
        }

        public static void EqualTime(TimeSpan expected, TimeSpan actual, string message)
        {
            var expectedSeconds = Math.Round(expected.TotalSeconds);
            var actualSeconds = Math.Round(actual.TotalSeconds);

            Assert.True(expectedSeconds == Math.Round(actual.TotalSeconds), message + $"({expectedSeconds}, {actualSeconds}); ({expected.TotalSeconds}; {actual.TotalSeconds})");
        }

        public static void EqualDate(DateTime expected, DateTime actual, string message)
        {
            Assert.Equal(expected.Date, actual.Date);
            var expectedSeconds = Math.Round(expected.TimeOfDay.TotalSeconds);
            var actualSeconds = Math.Round(actual.TimeOfDay.TotalSeconds);

            Assert.True(expectedSeconds == Math.Round(actual.TimeOfDay.TotalSeconds), message + $"({expectedSeconds}, {actualSeconds}); ({expected.TimeOfDay.TotalSeconds}; {actual.TimeOfDay.TotalSeconds})");
        }
    }
}

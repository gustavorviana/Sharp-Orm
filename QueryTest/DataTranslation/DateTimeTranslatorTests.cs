using BaseTest.Utils;
using Bogus;
using SharpOrm.DataTranslation;

namespace QueryTest.DataTranslation
{
    public class DateTimeTranslatorTests : DbMockTest
    {
        private TranslationRegistry Registry => Config.Translation;

        [Fact]
        public void DateOnlyToSqlValue()
        {
            // Arrange
            var faker = new Faker();
            var expected = faker.Date.Recent().Date;

            // Act
            var result = Registry.ToSql(DateOnly.FromDateTime(expected), typeof(DateTime));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TimeOnlyToSqlValue()
        {
            // Arrange
            var faker = new Faker();
            var expected = faker.Date.Recent().TimeOfDay;

            // Act
            var result = Registry.ToSql(TimeOnly.FromTimeSpan(expected), typeof(TimeSpan));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DateOnlyFromUtcSqlDateNoTime()
        {
            // Arrange
            var faker = new Faker();
            var date = faker.Date.Recent().Date.SetKind(DateTimeKind.Utc);
            var expected = DateOnly.FromDateTime(date);

            // Act
            var result = Registry.FromSql(expected, typeof(DateOnly));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DateOnlyFromSqlDateTime()
        {
            // Arrange
            var faker = new Faker();
            var date = faker.Date.Recent().SetKind(DateTimeKind.Utc);
            var expected = DateOnly.FromDateTime(date);

            // Act
            var result = Registry.FromSql(expected, typeof(DateOnly));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TimeOnlyFromSqlUtcDateTimeOffset()
        {
            // Arrange
            var faker = new Faker();
            var registry = new TranslationRegistry
            {
                DbTimeZone = TimeZoneInfo.Utc,
                TimeZone = faker.TimeZoneInfo()
            };

            var fakeDate = faker.Date.Recent().RemoveMiliseconds().SetKind(DateTimeKind.Utc);
            var expected = TimeOnly.FromTimeSpan(TimeZoneInfo.ConvertTime(fakeDate, registry.TimeZone).TimeOfDay);

            // Act
            var result = registry.FromSql(new DateTimeOffset(fakeDate, TimeSpan.Zero), typeof(TimeOnly));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TimeOnlyFromSqlUtcDateTime()
        {
            // Arrange
            var faker = new Faker();

            var fakeDate = faker.Date.Recent().RemoveMiliseconds().SetKind(DateTimeKind.Utc);
            var expected = TimeOnly.FromTimeSpan(fakeDate.TimeOfDay);

            // Act
            var result = Registry.FromSql(fakeDate, typeof(TimeOnly));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TimeOnlyFromSqlTimeSpan()
        {
            // Arrange
            var faker = new Faker();

            var fakeTime = faker.Date.Recent().RemoveMiliseconds().SetKind(DateTimeKind.Utc).TimeOfDay;
            var expected = TimeOnly.FromTimeSpan(fakeTime);

            // Act
            var result = Registry.FromSql(fakeTime, typeof(TimeOnly));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TimeSpanFromSqlDate()
        {
            // Arrange
            var faker = new Faker();

            var fakeTime = faker.Date.Recent().RemoveMiliseconds().SetKind(DateTimeKind.Utc);
            var expected = fakeTime.TimeOfDay;

            // Act
            var result = Registry.FromSql(fakeTime, typeof(TimeSpan));

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CustomDateFormat()
        {
            Registry.DateFormat = "dd:MM:yyyy (HH-mm-ss)";
            var result = Registry.FromSql("01:04:2025 (14-32-13)", typeof(DateTime));
            Assert.NotNull(result);
        }

        [Theory]
        [ClassData(typeof(DateTestData))]
        public void DateStringConverter(string stringDate, DateTime expectedDate)
        {
            Registry.Culture = System.Globalization.CultureInfo.InvariantCulture;
            var result = Registry.FromSql(stringDate, typeof(DateTime));
            Assert.Equal(expectedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), ((DateTime)result).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }

        public class DateTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "April 1, 2025", new DateTime(2025, 4, 1) };
                yield return new object[] { "Tuesday, April 1, 2025", new DateTime(2025, 4, 1) };
                yield return new object[] { "01/04/2025", new DateTime(2025, 4, 1) };
                yield return new object[] { "2025-04-01", new DateTime(2025, 4, 1) };
                yield return new object[] { "01/04/2025 02:32 PM", new DateTime(2025, 4, 1, 14, 32, 0) };
                yield return new object[] { "April 1, 2025 14:32:13", new DateTime(2025, 4, 1, 14, 32, 13) };
                yield return new object[] { "April 1, 2025 02:32 PM", new DateTime(2025, 4, 1, 14, 32, 0) };
                yield return new object[] { "April 1, 2025 02:32:13 PM", new DateTime(2025, 4, 1, 14, 32, 13) };
                yield return new object[] { "01/04/2025 14:32:13", new DateTime(2025, 4, 1, 14, 32, 13) };
                yield return new object[] { "2025-04-01 14:32:13", new DateTime(2025, 4, 1, 14, 32, 13) };
                yield return new object[] { "2025-04-01T14:32:13.34", new DateTime(2025, 4, 1, 14, 32, 13, 340) };
                yield return new object[] { "2025-04-01 14:32:13.34", new DateTime(2025, 4, 1, 14, 32, 13, 340) };
                yield return new object[] { "2025-04-01T14:32:13.34Z", new DateTime(2025, 4, 1, 11, 32, 13, 340) };
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}

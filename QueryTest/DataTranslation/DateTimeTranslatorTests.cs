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
    }
}

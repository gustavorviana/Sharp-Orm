using Bogus;

namespace BaseTest.Utils
{
    public static class FakerExtension
    {
        private const string UtcName = "UTC";
        public static TimeZoneInfo TimeZoneInfo(this Faker faker, bool allowUtc = false)
        {
            IList<TimeZoneInfo> timeZones = System.TimeZoneInfo.GetSystemTimeZones();

            if (!allowUtc) timeZones = timeZones.Where(x => x.Id != UtcName).ToArray();
            return timeZones[faker.Random.Int(0, timeZones.Count)];
        }
    }
}

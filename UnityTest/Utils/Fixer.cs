using System;

namespace UnityTest.Utils
{
    internal static class Fixer
    {
        public static DateTime RemoveMiliseconds(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        }
    }
}

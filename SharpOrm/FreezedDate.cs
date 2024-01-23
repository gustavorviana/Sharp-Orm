using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// It represents a date that should not have its value changed when the TimeZoneInfo of the code or database is set.
    /// </summary>
    public struct FreezedDate
    {
        public DateTime Value { get; }

        public FreezedDate(DateTime value)
        {
            this.Value = value;
        }

        public static FreezedDate Now => new FreezedDate(DateTime.Now);
        public static FreezedDate UtcNow => new FreezedDate(DateTime.UtcNow);
    }
}

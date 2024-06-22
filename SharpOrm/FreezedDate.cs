using System;

namespace SharpOrm
{
    /// <summary>
    /// It represents a date that should not have its value changed when the TimeZoneInfo of the code or database is set.
    /// </summary>
    public struct FreezedDate
    {
        /// <summary>
        /// Gets the frozen date value.
        /// </summary>
        public DateTime Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FreezedDate"/> class with the specified date value.
        /// </summary>
        /// <param name="value">The date value to be frozen.</param>
        public FreezedDate(DateTime value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets a <see cref="FreezedDate"/> object that represents the current date and time.
        /// </summary>
        public static FreezedDate Now => new FreezedDate(DateTime.Now);

        /// <summary>
        /// Gets a <see cref="FreezedDate"/> object that represents the current date.
        /// </summary>
        public static FreezedDate Today => new FreezedDate(DateTime.Today);

        /// <summary>
        /// Gets a <see cref="FreezedDate"/> object that represents the current date and time in Coordinated Universal Time (UTC).
        /// </summary>
        public static FreezedDate UtcNow => new FreezedDate(DateTime.UtcNow);
    }
}

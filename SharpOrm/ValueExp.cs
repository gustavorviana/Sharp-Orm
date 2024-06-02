namespace SharpOrm
{
    /// <summary>
    /// Represents a SQL expression that contains a value.
    /// </summary>
    public class ValueExp : SqlExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueExp"/> class with the specified value.
        /// </summary>
        /// <param name="value">The value to be represented by the SQL expression.</param>
        public ValueExp(object value) : base("?", value)
        {
        }
    }

}

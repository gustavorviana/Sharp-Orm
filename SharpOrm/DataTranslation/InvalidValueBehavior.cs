namespace SharpOrm.DataTranslation
{
    /// <summary>
    /// Specifies the behavior when an invalid value is encountered during conversion.
    /// </summary>
    public enum InvalidValueBehavior
    {
        /// <summary>
        /// Throw an exception when an invalid value is encountered.
        /// </summary>
        ThrowException,

        /// <summary>
        /// Return the default value for the type when an invalid value is encountered.
        /// </summary>
        ReturnDefault
    }
}


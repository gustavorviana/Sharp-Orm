using System;

namespace SharpOrm.Builder.DataTranslation
{
    public interface ISqlTranslation
    {
        /// <summary>
        /// Signals whether the class can work with the type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool CanWork(Type type);

        /// <summary>
        /// Translates a sql value to a c# value.
        /// </summary>
        /// <param name="value">Value received from sql.</param>
        /// <param name="expectedType">Type of value that the receiver accepts to receive.</param>
        /// <returns></returns>
        object FromSqlValue(object value, Type expectedType);

        /// <summary>
        /// Translate a c# object to sql.
        /// </summary>
        /// <param name="value">Value that will be sent to sql.</param>
        /// <param name="type">Type of value that the column is sending.</param>
        /// <returns></returns>
        object ToSqlValue(object value, Type type);
    }
}
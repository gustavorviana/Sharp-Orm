﻿using System;

namespace SharpOrm.Builder.DataTranslation
{
    public interface ISqlTranslation
    {
        /// <summary>
        /// Determines whether the translator can work with the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the translator can work with the type; otherwise, <c>false</c>.</returns>
        bool CanWork(Type type);

        /// <summary>
        /// Translates a sql value to a c# value.
        /// </summary>
        /// <param name="value">Value received from sql.</param>
        /// <param name="expectedType">Expected type.</param>
        /// <returns></returns>
        object FromSqlValue(object value, Type expectedType);

        /// <summary>
        /// Translate a c# object to sql.
        /// </summary>
        /// <param name="value">Value that will be sent to sql.</param>
        /// <param name="type">Expected type.</param>
        /// <returns></returns>
        object ToSqlValue(object value, Type type);
    }
}
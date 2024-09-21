﻿using SharpOrm.DataTranslation;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharpOrm.Builder
{
    internal interface IColumnInfo
    {
        ValidationAttribute[] Validations { get; }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the column is a primary key.
        /// </summary>
        bool Key { get; }

        /// <summary>
        /// Gets the order of the column.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets the SQL translation for the column.
        /// </summary>
        ISqlTranslation Translation { get; }

        /// <summary>
        /// Gets the foreign key info of the column.
        /// </summary>
        ForeignAttribute ForeignInfo { get; }

        /// <summary>
        /// Gets a value indicating whether the column name is auto-generated.
        /// </summary>
        bool AutoGenerated { get; }
    }
}

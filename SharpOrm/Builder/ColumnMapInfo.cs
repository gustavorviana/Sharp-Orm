﻿using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder
{
    public class ColumnMapInfo : IColumnInfo
    {
        #region Fields
        internal bool builded;
        private List<ValidationAttribute> _validations = new List<ValidationAttribute>();
        internal string _name;
        private bool _key;
        private int _order;
        private Type _type;
        private ISqlTranslation _translation;
        private ForeignAttribute _foreignInfo;
        private bool _autoGenerated;
        internal MapNestedAttribute _mapNested;
        #endregion

        ValidationAttribute[] IColumnInfo.Validations => _validations.ToArray();

        string IColumnInfo.Name => _name;

        bool IColumnInfo.Key => _key;

        int IColumnInfo.Order => _order;

        ISqlTranslation IColumnInfo.Translation => _translation;

        ForeignAttribute IColumnInfo.ForeignInfo => _foreignInfo;

        bool IColumnInfo.AutoGenerated => _autoGenerated;

        MapNestedAttribute IColumnInfo.MapNested => _mapNested;

        internal ColumnMapInfo(MemberInfo member)
        {
            this._translation = TranslationRegistry.GetOf(member);

            ColumnAttribute colAttr = member.GetCustomAttribute<ColumnAttribute>();

            this._validations.AddRange(member.GetCustomAttributes<ValidationAttribute>());
            this._autoGenerated = string.IsNullOrEmpty(colAttr?.Name);
            this._key = ColumnInfo.Iskey(member);
            this._order = colAttr?.Order ?? -1;
        }

        /// <summary>
        /// Sets the name of the column in the database..
        /// </summary>
        /// <param name="name">The name to set for the column</param>
        /// <returns></returns>
        public ColumnMapInfo HasColumnName(string name)
        {
            this.CheckBuilded();

            this._name = name;
            this._autoGenerated = false;

            return this;
        }

        /// <summary>
        /// Sets the SQL translation for the column.
        /// </summary>
        /// <param name="translation">The SQL translation to set for the column.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ColumnMapInfo SetTranslation(ISqlTranslation translation)
        {
            this.CheckBuilded();

            this._translation = translation ?? throw new ArgumentNullException(nameof(translation));

            return this;
        }

        /// <summary>
        /// Sets the foreign key relationship for the column.
        /// </summary>
        /// <param name="foreignKey">The foreign key column name.</param>
        /// <param name="localKey">The local key column name. Defaults to "id".</param>
        /// <returns></returns>
        public ColumnMapInfo SetForeign(string foreignKey, string localKey = "id")
        {
            this.CheckBuilded();

            this._foreignInfo = new ForeignAttribute(foreignKey) { LocalKey = localKey };

            return this;
        }

        /// <summary>
        /// Denotes a property that uniquely identifies an entity.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ColumnMapInfo SetKey(bool value)
        {
            this.CheckBuilded();

            this._key = value;

            return this;
        }

        /// <summary>
        /// Specifies that a data field value is required.
        /// </summary>
        /// <returns></returns>
        public ColumnMapInfo IsRequired()
        {
            if (!this.HasValidation<RequiredAttribute>())
                this._validations.Add(new RequiredAttribute());

            return this;
        }

        /// <summary>
        /// Specifies the minimum length of array or string data allowed in a property
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ColumnMapInfo HasMinLength(int length)
        {
            if (!this.HasValidation<MinLengthAttribute>())
                this._validations.Add(new MinLengthAttribute(length));

            return this;
        }

        /// <summary>
        /// Specifies the maximum length of array or string data allowed in a property.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public ColumnMapInfo HasMaxLength(int length)
        {
            if (!this.HasValidation<MaxLengthAttribute>())
                this._validations.Add(new MaxLengthAttribute(length));

            return this;
        }

        private bool HasValidation<T>() where T : ValidationAttribute
        {
            this.CheckBuilded();

            return this._validations.Any(x => x is T);
        }

        /// <summary>
        /// Sets the validation attributes for the column.
        /// </summary>
        /// <param name="validations"></param>
        /// <returns></returns>
        public ColumnMapInfo SetValidations(params ValidationAttribute[] validations)
        {
            this.CheckBuilded();

            this._validations.Clear();
            this._validations.AddRange(validations);
            return this;
        }

        private void CheckBuilded()
        {
            if (this.builded)
                throw new InvalidOperationException("It is not possible to alter the column; it has already been built.");
        }
    }
}

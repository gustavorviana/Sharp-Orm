﻿using SharpOrm.DataTranslation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SharpOrm.Builder
{
    public class ColumnMapInfo : IColumnInfo
    {
        #region Fields
        internal bool builded;
        private ValidationAttribute[] _validations;
        internal string _name;
        private bool _key;
        private int _order;
        private Type _type;
        private ISqlTranslation _translation;
        private ForeignAttribute _foreignInfo;
        private bool _autoGenerated;
        #endregion

        ValidationAttribute[] IColumnInfo.Validations => _validations;

        string IColumnInfo.Name => _name;

        bool IColumnInfo.Key => _key;

        int IColumnInfo.Order => _order;

        ISqlTranslation IColumnInfo.Translation => _translation;

        ForeignAttribute IColumnInfo.ForeignInfo => _foreignInfo;

        bool IColumnInfo.AutoGenerated => _autoGenerated;

        internal ColumnMapInfo(MemberInfo member)
        {
            this._translation = TranslationRegistry.GetOf(member);

            ColumnAttribute colAttr = member.GetCustomAttribute<ColumnAttribute>();

            this._autoGenerated = string.IsNullOrEmpty(colAttr?.Name);
            this._key = ColumnInfo.Iskey(member);
            this._order = colAttr?.Order ?? -1;
        }

        public ColumnMapInfo SetColumn(string name)
        {
            this.CheckBuilded();

            this._name = name;
            this._autoGenerated = false;

            return this;
        }

        public ColumnMapInfo SetTranslation(ISqlTranslation translation)
        {
            this.CheckBuilded();

            this._translation = translation ?? throw new ArgumentNullException(nameof(translation));

            return this;
        }

        public ColumnMapInfo SetForeign(string foreignKey, string localKey = "id")
        {
            this.CheckBuilded();

            this._foreignInfo = new ForeignAttribute(foreignKey) { LocalKey = localKey };

            return this;
        }

        public ColumnMapInfo SetKey(bool value)
        {
            this.CheckBuilded();

            this._key = value;

            return this;
        }

        public ColumnMapInfo SetValidations(params ValidationAttribute[] validations)
        {
            this.CheckBuilded();

            this._validations = validations;
            return this;
        }

        private void CheckBuilded()
        {
            if (this.builded)
                throw new InvalidOperationException("It is not possible to alter the column; it has already been built.");
        }
    }
}

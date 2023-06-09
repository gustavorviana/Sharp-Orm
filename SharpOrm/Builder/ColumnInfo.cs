﻿using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents information about a column in a database table.
    /// </summary>
    public class ColumnInfo : IEquatable<ColumnInfo>
    {
        #region Properties
        private readonly MemberInfo column;

        private readonly TranslationRegistry config;

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the column is a primary key.
        /// </summary>
        public bool Key { get; }

        /// <summary>
        /// Gets the order of the column.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the column is required.
        /// </summary>
        public bool Required { get; }

        /// <summary>
        /// Gets the type of the declaring class.
        /// </summary>
        public Type DeclaringType { get; }

        /// <summary>
        /// Gets the type of the column.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the SQL translation for the column.
        /// </summary>
        public ISqlTranslation Translation { get; }

        /// <summary>
        /// Gets the foreign key name of the column.
        /// </summary>
        public string ForeignKey { get; }

        /// <summary>
        /// Gets a value indicating whether the column is auto-generated.
        /// </summary>
        public bool AutoGenerated { get; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnInfo"/> class for a field.
        /// </summary>
        /// <param name="registry">The translation registry.</param>
        /// <param name="fieldInfo">The field information.</param>
        public ColumnInfo(TranslationRegistry registry, FieldInfo fieldInfo) : this(registry, registry.GetOf(fieldInfo), fieldInfo)
        {
            this.Type = fieldInfo.FieldType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnInfo"/> class for a property.
        /// </summary>
        /// <param name="registry">The translation registry.</param>
        /// <param name="propertyInfo">The property information.</param>
        public ColumnInfo(TranslationRegistry registry, PropertyInfo propertyInfo) : this(registry, registry.GetOf(propertyInfo), propertyInfo)
        {
            this.Type = propertyInfo.PropertyType;
        }

        private ColumnInfo(TranslationRegistry registry, ISqlTranslation translation, MemberInfo member)
        {
            this.config = registry;
            this.column = member;
            this.Translation = translation;
            this.DeclaringType = member.DeclaringType;

            this.ForeignKey = this.GetAttribute<ForeignKeyAttribute>()?.Name;
            this.Required = this.GetAttribute<RequiredAttribute>() != null;

            ColumnAttribute colAttr = this.GetAttribute<ColumnAttribute>();

            this.AutoGenerated = string.IsNullOrEmpty(colAttr?.Name);
            this.Name = colAttr?.Name ?? member.Name;
            this.Order = colAttr?.Order ?? int.MaxValue;

            this.Key = this.GetAttribute<KeyAttribute>() != null || this.Name.ToLower() == "id";
        }

        /// <summary>
        /// Gets the specified attribute applied to the column.
        /// </summary>
        /// <typeparam name="T">The type of the attribute.</typeparam>
        /// <returns>The attribute instance if found; otherwise, <c>null</c>.</returns>
        public T GetAttribute<T>() where T : Attribute
        {
            return this.column.GetCustomAttribute<T>();
        }

        /// <summary>
        /// Sets the value of the column for the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="value">The value to set.</param>
        public void Set(object owner, object value)
        {
            this.SetRaw(owner, this.ParseValue(value, false));
        }

        /// <summary>
        /// Sets the raw value of the column for the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <param name="value">The raw value to set.</param>
        public void SetRaw(object owner, object value)
        {
            if (this.column is FieldInfo field)
                field.SetValue(owner, value);
            else ((PropertyInfo)this.column).SetValue(owner, value);
        }

        /// <summary>
        /// Gets the value of the column for the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <returns>The value of the column.</returns>
        public object Get(object owner)
        {
            return this.ParseValue(this.GetRaw(owner), true);
        }

        /// <summary>
        /// Gets the raw value of the column for the specified owner object.
        /// </summary>
        /// <param name="owner">The owner object.</param>
        /// <returns>The raw value of the column.</returns>
        public object GetRaw(object owner)
        {
            if (this.column is FieldInfo field)
                return field.GetValue(owner);

            return ((PropertyInfo)this.column).GetValue(owner);
        }

        private object ParseValue(object value, bool toDb)
        {
            if (toDb)
                return this.Translation == null ?
                        this.config.ToSql(value) :
                        this.Translation.ToSqlValue(value, this.Type);

            return this.Translation == null ?
                    this.config.FromSql(value, this.Type) :
                    this.Translation.FromSqlValue(value, this.Type);
        }

        /// <summary>
        /// Gets the name of the specified member.
        /// </summary>
        /// <param name="member">The member information.</param>
        /// <returns>The name of the member.</returns>
        public static string GetName(MemberInfo member)
        {
            return member.GetCustomAttribute<ColumnAttribute>()?.Name ?? member.Name;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.Type);
        }

        #region IEquatable
        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnInfo);
        }

        public bool Equals(ColumnInfo other)
        {
            return !(other is null) &&
                   EqualityComparer<MemberInfo>.Default.Equals(column, other.column) &&
                   EqualityComparer<TranslationRegistry>.Default.Equals(config, other.config) &&
                   Name == other.Name &&
                   Key == other.Key &&
                   Order == other.Order &&
                   Required == other.Required &&
                   EqualityComparer<Type>.Default.Equals(DeclaringType, other.DeclaringType) &&
                   EqualityComparer<Type>.Default.Equals(Type, other.Type) &&
                   EqualityComparer<ISqlTranslation>.Default.Equals(Translation, other.Translation) &&
                   ForeignKey == other.ForeignKey &&
                   AutoGenerated == other.AutoGenerated;
        }

        public override int GetHashCode()
        {
            int hashCode = -1825907665;
            hashCode = hashCode * -1521134295 + EqualityComparer<MemberInfo>.Default.GetHashCode(column);
            hashCode = hashCode * -1521134295 + EqualityComparer<TranslationRegistry>.Default.GetHashCode(config);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Key.GetHashCode();
            hashCode = hashCode * -1521134295 + Order.GetHashCode();
            hashCode = hashCode * -1521134295 + Required.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(DeclaringType);
            hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<ISqlTranslation>.Default.GetHashCode(Translation);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ForeignKey);
            hashCode = hashCode * -1521134295 + AutoGenerated.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ColumnInfo left, ColumnInfo right)
        {
            return EqualityComparer<ColumnInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(ColumnInfo left, ColumnInfo right)
        {
            return !(left == right);
        }
        #endregion
    }
}

using System;
using System.Linq;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Represents the name of an element in the database.
    /// </summary>
    public struct DbName
    {
        /// <summary>
        /// Gets or sets the alias.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the alias.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbName"/> struct with the specified name and alias.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="alias">The alias.</param>
        /// <param name="validateChars">If true, throw an error if the name contains invalid or unsafe characters.</param>
        /// <remarks>Valid characters for name: A-Z, 0-9, '#', '_', and '.'. Valid characters for alias: A-Z, 0-9, '#', '_', ' ', and '.'</remarks>
        public DbName(string name, string alias, bool validateChars = true)
        {
            if (validateChars)
            {
                ValidateName(name);
                ValidateAlias(alias);
            }

            this.Name = name;
            this.Alias = alias;
        }

        public static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (!IsValid(name, '.', '_', '#'))
                throw new InvalidOperationException("The name contains one or more invalid characters.");
        }

        public static void ValidateAlias(string alias)
        {
            if (!string.IsNullOrEmpty(alias) && !IsValid(alias, '.', '_', ' ', '.'))
                throw new InvalidOperationException("The alias contains one or more invalid characters.");
        }

        public static DbName Of<T>(string alias)
        {
            if (ReflectionUtils.IsDynamic(typeof(T)))
                throw new NotSupportedException("It is not possible to use dynamic types in this operation.");

            return new DbName(TableInfo.GetNameOf(typeof(T)), alias);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbName"/> struct with the specified full name.
        /// </summary>
        /// <param name="fullName">The full name.</param>
        public DbName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                throw new ArgumentNullException(nameof(fullName));

            var splits = fullName.Split(' ');
            if (splits.Length > 3)
                throw new ArgumentException("Table name is invalid.");

            this.Name = splits[0];
            this.Alias = GetAlias(splits);
        }

        /// <summary>
        /// Tries to retrieve the alias of the object.
        /// </summary>
        /// <returns>The alias of the object, if set; otherwise, returns the name of the object.</returns>
        public string TryGetAlias(QueryConfig config)
        {
            return string.IsNullOrEmpty(this.Alias) ?
                config.ApplyNomenclature(this.Name) :
                config.ApplyNomenclature(this.Alias);
        }

        /// <summary>
        /// Gets the name of the object, optionally including the alias.
        /// </summary>
        /// <param name="withAlias">Specifies whether to include the alias in the name.</param>
        /// <param name="config">The query configuration used to apply nomenclature.</param>
        /// <returns>The name of the object, with or without the alias, based on the specified parameters.</returns>
        public string GetName(bool withAlias, QueryConfig config)
        {
            if (!withAlias || string.IsNullOrEmpty(this.Alias) || this.Alias == this.Name)
                return config.ApplyNomenclature(this.Name);

            return string.Concat(config.ApplyNomenclature(this.Name), " ", config.ApplyNomenclature(this.Alias));
        }

        private static string GetAlias(string[] split)
        {
            if (split.Length == 1)
                return null;

            if (split.Length == 2)
                return split[1];

            return split[2];
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(this.Alias) ? this.Name : this.Alias;
        }

        private static bool IsValid(string content, params char[] allowed)
        {
            return content.All(c => char.IsLetterOrDigit(c) || allowed.Contains(c));
        }
    }
}
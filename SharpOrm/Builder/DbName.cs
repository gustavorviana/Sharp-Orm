using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
        public DbName(string name, string alias)
        {
            this.Name = name;
            this.Alias = alias;
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
    }
}
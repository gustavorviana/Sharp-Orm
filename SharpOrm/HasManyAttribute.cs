using System;

namespace SharpOrm
{
    /// <summary>
    /// Indicate that the property represents a list from another table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class HasManyAttribute : Attribute
    {
        /// <summary>
        /// Name of the column in the other table.
        /// </summary>
        public string ForeignKey { get; set; }
        /// <summary>
        /// Name of the column in the current table that should be used to retrieve the values.
        /// </summary>
        public string LocalKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="foreignKey">Name of the column in the other table.</param>
        /// <param name="localKey">Name of the column in the current table that should be used to retrieve the values.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public HasManyAttribute(string foreignKey, string localKey = "id")
        {
            if (string.IsNullOrEmpty(foreignKey))
                throw new ArgumentNullException(nameof(foreignKey));

            if (string.IsNullOrEmpty(localKey))
                throw new ArgumentNullException(nameof(localKey));

            this.ForeignKey = foreignKey;
            this.LocalKey = localKey;
        }
    }
}

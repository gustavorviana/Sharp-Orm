using System;

namespace SharpOrm
{
    /// <summary>
    /// Attribute to specify the relationship between two entities.
    /// Used to mark a property as a foreign key in a class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ForeignAttribute : Attribute
    {
        /// <summary>
        /// Name of the column in the child table.
        /// </summary>
        public string ForeignKey { get; }

        /// <summary>
        /// Name of the column in the current table.
        /// </summary>
        public string LocalKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="foreignKey">Name of the column in the other table.</param>
        /// <param name="localKey">Name of the column in the current table.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ForeignAttribute(string foreignKey)
        {
            if (string.IsNullOrEmpty(foreignKey))
                throw new ArgumentNullException(nameof(foreignKey));

            this.ForeignKey = foreignKey;
        }
    }
}

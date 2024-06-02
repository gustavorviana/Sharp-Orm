using System;

namespace SharpOrm
{
    /// <summary>
    /// Represents an attribute used to specify the name of a column in a constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CtorColumnAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CtorColumnAttribute"/> class with the specified column name.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        public CtorColumnAttribute(string name)
        {
            this.Name = name;
        }
    }
}

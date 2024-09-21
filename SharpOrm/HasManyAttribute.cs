using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharpOrm
{
    /// <summary>
    /// Indicate that the property represents a list from another table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    [Obsolete("Use SharpOrm.ForeignAttribute. This attribute will be removed in version 3.x.")]
    public class HasManyAttribute : ForeignAttribute
    {
        public HasManyAttribute(string foreignKey, string localKey = "id") : base(foreignKey)
        {
            this.LocalKey = localKey;
        }
    }
}

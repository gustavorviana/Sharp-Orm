using System;

namespace SharpOrm
{
    [AttributeUsage(AttributeTargets.Property)]
    public class HasManyAttribute : Attribute
    {
        public string ForeignKey { get; set; }
        public string LocalKey { get; set; }

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

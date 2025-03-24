using System;

namespace SharpOrm
{
    /// <summary>
    /// Indicates that the property should be mapped along with its nested objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MapNestedAttribute : Attribute
    {
        public string Prefix { get; set; }
    }
}

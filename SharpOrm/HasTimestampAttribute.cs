using System;

namespace SharpOrm
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HasTimestampAttribute : Attribute
    {
        public string CreatedAtColumn { get; set; } = "CreatedAt";
        public string UpdatedAtColumn { get; set; } = "UpdatedAt";
    }
}

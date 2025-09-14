using System;

namespace SharpOrm
{
    /// <summary>
    /// Indicates that a class is an owned entity that should be flattened into the parent table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class OwnedAttribute : Attribute
    {
    }
}
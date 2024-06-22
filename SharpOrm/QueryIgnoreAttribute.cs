using System;

namespace SharpOrm
{
    /// <summary>
    /// Prevent the constructor from being used to instantiate the object by Query<T>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public class QueryIgnoreAttribute : Attribute
    {
    }
}

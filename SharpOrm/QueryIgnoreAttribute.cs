using System;
using System.Collections.Generic;
using System.Text;

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

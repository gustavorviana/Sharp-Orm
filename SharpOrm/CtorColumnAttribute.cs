using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm
{
    /// <summary>
    /// Name of the column that should populate the parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CtorColumnAttribute : Attribute
    {
        public string Name { get; }

        public CtorColumnAttribute(string name)
        {
            this.Name = name;
        }
    }
}

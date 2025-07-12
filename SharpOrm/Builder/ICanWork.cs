using System;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    /// <summary>
    /// Interface that defines the ability to determine if an implementation can work with a specific type.
    /// </summary>
    public interface ICanWork<K>
    {
        /// <summary>
        /// Determines if the implementation can work with the specified type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the implementation can work with the type; otherwise, false.</returns>
        bool CanWork(K type);
    }
}

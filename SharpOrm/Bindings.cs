using System.Reflection;

namespace SharpOrm
{
    internal static class Bindings
    {
        /// <summary>
        /// Binding public and private members.
        /// </summary>
        public static readonly BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Binding public members.
        /// </summary>
        public static readonly BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
    }
}

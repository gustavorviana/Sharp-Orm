using System;
using System.Reflection;

namespace SharpOrm
{
    internal class AnnotationsAssemblyRedirector
    {
        public const string AssemblyName = "System.ComponentModel.Annotations";

        public static bool NeedLoad()
        {
            return Type.GetType(AssemblyName + ".TableAttribute") == null;
        }

        public static void LoadRedirector()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyOnCurrentDomain;
        }

        private static Assembly ResolveAssemblyOnCurrentDomain(object sender, ResolveEventArgs args)
        {
            if (!args.Name.StartsWith(AssemblyName))
                return null;

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssemblyOnCurrentDomain;
            return Assembly.Load(AssemblyName);
        }
    }
}

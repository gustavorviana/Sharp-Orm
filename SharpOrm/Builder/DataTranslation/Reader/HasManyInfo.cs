using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    internal class HasManyInfo : ForeignInfo
    {
        public string LocalKey { get; }

        public HasManyInfo(Type type, object foreignKey, int depth, string localKey) : base(type, foreignKey, depth)
        {
            this.LocalKey = localKey;
        }

        public IList CreateList()
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(HasManyInfo.GetGenericArg(this.Type)));
        }

        public static Type GetGenericArg(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.GetGenericArguments() is Type[] types && types.Length > 0 && types[0] != typeof(object))
                return types[0];

            return typeof(Row);
        }

        public static bool IsCollection(Type type)
        {
            if (type.IsArray)
                return true;

            if (!type.IsGenericType)
                return false;

            type = type.GetGenericTypeDefinition();
            return type == typeof(IList<>) || type == typeof(List<>);
        }
    }
}

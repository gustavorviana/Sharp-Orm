﻿using SharpOrm.Builder.DataTranslation.Reader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpOrm.Builder
{
    internal static class ReflectionUtils
    {
        public static Array ToArray(Type type, ICollection collection)
        {
            Array array = Array.CreateInstance(type, collection.Count);
            collection.CopyTo(array, 0);
            return array;
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

        public static IList CreateList(Type type)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(GetGenericArg(type)));
        }

        public static Type GetGenericArg(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.GetGenericArguments() is Type[] types && types.Length > 0 && types[0] != typeof(object))
                return types[0];

            return typeof(Row);
        }
    }
}

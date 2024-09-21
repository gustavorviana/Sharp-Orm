using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder
{
    internal static class ReflectionUtils
    {
        public static Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo prop) return prop.PropertyType;
            if (member is FieldInfo field) return field.FieldType;

            throw new NotSupportedException();
        }

        public static bool IsDynamic(Type type)
        {
            return type == typeof(object) || type == typeof(ExpandoObject);
        }

        public static string ToPath(IList<MemberInfo> path, char pathChar = '.', int? limit = null)
        {
            if (path.Count == 0) return string.Empty;

            StringBuilder b = new StringBuilder(path[0].Name);
            int count = limit ?? path.Count;

            for (int i = 1; i < count; i++)
                b.Append(pathChar).Append(path[i].Name);

            return b.ToString();
        }

        public static object GetMemberValue(MemberInfo member, object owner)
        {
            if (member is FieldInfo field)
                return field.GetValue(owner);

            return ((PropertyInfo)member).GetValue(owner);
        }

        public static void SetMemberValue(MemberInfo member, object owner, object value)
        {
            if (member is FieldInfo field) field.SetValue(owner, value);
            else ((PropertyInfo)member).SetValue(owner, value);
        }

        public static void AddToArray<T>(ref T[] array, IList<T> items)
        {
            int lastSize = array.Length;
            Array.Resize(ref array, array.Length + items.Count);

            for (int i = 0; i < items.Count; i++)
                array[lastSize + i] = items[i];
        }

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
            return type == typeof(IList<>) || type == typeof(List<>) || type == typeof(IList);
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

            return type;
        }

        public static void CopyPropTo<T>(T source, T target, PropertyInfo srcProp)
        {
            if (target.GetType().GetProperty(srcProp.Name) is PropertyInfo targetProp && targetProp.CanWrite)
                targetProp.SetValue(target, srcProp.GetValue(source));
        }
    }
}

using System;
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

        public static Type GetTopMostBaseType(Type type)
        {
            if (type == null) return null;

            Type current = type;
            while (current.BaseType != null && current.BaseType != typeof(object))
                current = current.BaseType;

            return current;
        }

        public static bool SameType(Type type1, Type type2)
        {
            return GetTopMostBaseType(type1) == GetTopMostBaseType(type2);
        }

        public static object GetMemberValue(MemberInfo member, object owner)
        {
            if (TryGetValue(member, owner, out object value))
                return value;

            throw new NotSupportedException();
        }

        public static bool TryGetValue(MemberInfo member, object owner, out object value)
        {
            if (member is FieldInfo field)
            {
                value = field.GetValue(owner);
                return true;
            }

            if (member is PropertyInfo property)
            {
                value = property.GetValue(owner);
                return true;
            }

            value = null;
            return false;
        }

        public static void SetMemberValue(MemberInfo member, object owner, object value)
        {
            if (member is FieldInfo field) field.SetValue(owner, value);
            else ((PropertyInfo)member).SetValue(owner, value);
        }

        public static bool IsCollection(Type type)
        {
            return RuntimeList.IsCollection(type);
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

        public static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool IsStatic(MemberInfo member)
        {
            if (member is PropertyInfo propertyInfo)
                return propertyInfo.GetMethod?.IsStatic ?? false;

            if (member is FieldInfo fieldInfo)
                return fieldInfo.IsStatic;

            if (member is MethodInfo methodInfo)
                return methodInfo.IsStatic;

            return false;

        }
    }
}

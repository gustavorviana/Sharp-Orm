using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder
{
    internal class RuntimeList
    {
        public readonly IList items;

        public RuntimeList(Type listType)
        {
            this.items = CreateList(listType);
        }

        public void AddAll(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
                items.Add(enumerator.Current);
        }

        private static IList CreateList(Type type)
        {
            if (IsAnyGenericType(type, typeof(List<>), typeof(Collection<>)))
                return NewInstance(type);

            return NewInstance(typeof(List<>).MakeGenericType(ReflectionUtils.GetGenericArg(type)));
        }

        private static IList NewInstance(Type listType)
        {
            return (IList)Activator.CreateInstance(listType);
        }

        public object ToCollection(Type target)
        {
            if (target == items.GetType()) return items;
            if (target.IsArray) return ToArray(target.GetElementType(), items);
            if (target.IsInterface) return ToCollectionByInterface(target);

            return Activator.CreateInstance(target, new object[] { items });
        }

        private object ToCollectionByInterface(Type target)
        {
            var interfaces = target.GetInterfaces();
            if (interfaces.Contains(typeof(IReadOnlyList<>)) || interfaces.Contains(typeof(IReadOnlyCollection<>)))
                return this.CreateWithItems(typeof(ReadOnlyCollection<>), ReflectionUtils.GetGenericArg(target));

            return this.CreateWithItems(typeof(List<>), ReflectionUtils.GetGenericArg(target));
        }

        private object CreateWithItems(Type list, Type genericArg)
        {
            return Activator.CreateInstance(list.MakeGenericType(genericArg), new object[] { items });
        }

        public static Array ToArray(Type type, ICollection collection)
        {
            Array array = Array.CreateInstance(type, collection.Count);
            collection.CopyTo(array, 0);
            return array;
        }

        internal static bool IsAnyGenericType(Type type, params Type[] genericTypes)
        {
            if (!type.IsGenericType) return false;

            var genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypes.Contains(genericTypeDefinition);
        }

        #region IsCollection
        public static bool IsCollection(Type type)
        {
            if (type.IsArray || IsGenericCollection(type))
                return true;

            if (GetCollectionInterfaces().Contains(type))
                return true;

            var interfaces = type.GetInterfaces();
            return GetCollectionInterfaces().Any(x => interfaces.Contains(x));
        }

        private static bool IsGenericCollection(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var typeDefinition = type.GetGenericTypeDefinition();
            if (GetGenericCollectionInterfaces().Contains(typeDefinition))
                return true;

            var interfaces = type.GetInterfaces();
            return GetGenericCollectionInterfaces().Any(x => HasGenericInterface(interfaces, x));
        }

        public static Type GetCollectionElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            var args = type.GetGenericArguments();
            return args.Length > 0 ? args[0] : type;
        }

        private static IEnumerable<Type> GetGenericCollectionInterfaces()
        {
            yield return typeof(ICollection<>);
            yield return typeof(IList<>);
            yield return typeof(IReadOnlyCollection<>);
            yield return typeof(IReadOnlyList<>);
        }

        private static IEnumerable<Type> GetCollectionInterfaces()
        {
            yield return typeof(IList);
            yield return typeof(ICollection);
        }

        private static bool HasGenericInterface(Type[] types, Type interfaceType)
        {
            return types.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == interfaceType);
        }
        #endregion
    }
}

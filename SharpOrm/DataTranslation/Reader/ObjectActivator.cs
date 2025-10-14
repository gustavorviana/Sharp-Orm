using SharpOrm.DataTranslation.Reader.Activator;
using SharpOrm.DataTranslation.Reader.NameResolvers;
using SharpOrm.Msg;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader
{
    /// <summary>
    /// Class responsible for creating instances of objects using reflection.
    /// The class will only create an instance of the object, but the values of its properties and fields will not be initialized (except for record types).
    /// </summary>
    internal class ObjectActivator
    {
        private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> _constructorCache = new ConcurrentDictionary<Type, ConstructorInfo[]>();
        private readonly HashSet<string> _parameters;
        private readonly ActivatorConstructor _ctor;
        public readonly Type _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectActivator"/> class.
        /// </summary>
        /// <param name="type">The _type of the object to be activated.</param>
        /// <param name="record">The data record used to retrieve the object's data.</param>
        /// <exception cref="NotSupportedException">Thrown if the _type is abstract or no suitable constructor is found.</exception>
        public ObjectActivator(Type type, IDataRecord record, TranslationRegistry registry, INameResolver resolver)
        {
            if (type.IsAbstract) throw new NotSupportedException(Messages.ObjectActivator.AbstractType);
            if (type.IsInterface) throw new NotSupportedException(Messages.ObjectActivator.InterfaceType);
            if (type.IsEnum) throw new NotSupportedException(Messages.ObjectActivator.EnumType);
            if (type.IsArray) throw new NotSupportedException(Messages.ObjectActivator.ArrayType);

            var names = new string[record.FieldCount];
            for (int i = 0; i < names.Length; i++)
                names[i] = record.GetName(i);

            _type = type;
            _ctor = GetConstructor(record, registry, resolver);

            if (_ctor == null) _parameters = new HashSet<string>();
            else _parameters = new HashSet<string>(_ctor.GetParameterNames(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the appropriate constructor for the _type based on the data record.
        /// Selection criteria (in order of priority):
        /// 1. Number of filled parameters (higher is better)
        /// 2. Number of required parameters filled (higher is better)
        /// 3. Number of total parameters (higher is better - "richer" constructor)
        /// </summary>
        /// <param name="record">The data record used to match constructor parameters.</param>
        /// <returns>The matched constructor.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no suitable constructor is found or when there is ambiguity.</exception>
        private ActivatorConstructor GetConstructor(IDataRecord record, TranslationRegistry registry, INameResolver resolver)
        {
            var constructors = new List<ActivatorConstructor>();
            var binder = new ParameterBinder(_type, registry, record, resolver);

            foreach (var ctor in GetCachedConstructors(_type))
                if (ActivatorConstructor.TryParse(binder, ctor, out var activatorCtor))
                    constructors.Add(activatorCtor);

            if (constructors.Count == 0)
                return null;
            //throw new InvalidOperationException($"Unable to create an instance of '{_type.FullName}'.");

            // Sort by:
            // 1. Total matches (parameters filled) - descending
            // 2. Required parameters filled - descending
            // 3. Total parameters (richer constructor) - ascending
            var orderedMatches = constructors
                .OrderByDescending(m => m.Matches)
                .ThenByDescending(m => m.TotalParameters - m.OptionalParameters)
                .ThenBy(m => m.TotalParameters)
                .ToList();

            var bestMatch = orderedMatches[0];

            if (orderedMatches.Count > 1)
            {
                var secondBest = orderedMatches[1];
                if (bestMatch.IsSame(secondBest) && bestMatch.Matches > 0)
                    throw new InvalidOperationException(
                        $"Ambiguous constructor selection for type '{_type.FullName}'. " +
                        $"Multiple constructors have the same number of matching parameters ({bestMatch.Matches}).");
            }

            return bestMatch;
        }

        /// <summary>
        /// Gets cached constructors for a type, including public and protected constructors, filtering out those with QueryIgnoreAttribute.
        /// </summary>
        /// <param name="type">The type to get constructors for.</param>
        /// <returns>Array of valid constructors.</returns>
        private static ConstructorInfo[] GetCachedConstructors(Type constructorType)
        {
            return _constructorCache.GetOrAdd(constructorType, type =>
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var validCtors = new List<ConstructorInfo>(constructors.Length);

                for (int i = 0; i < constructors.Length; i++)
                {
                    var ctor = constructors[i];
                    // Only include public and protected constructors without QueryIgnoreAttribute
                    if ((ctor.IsPublic || ctor.IsFamily) && ctor.GetCustomAttribute<QueryIgnoreAttribute>() == null)
                        validCtors.Add(ctor);
                }

                return validCtors.ToArray();
            });
        }

        /// <summary>
        /// Creates an instance of the object using the data from the data record.
        /// </summary>
        /// <returns>The created object instance.</returns>
        public object CreateInstance() => _ctor?.Invoke();

        public bool ContainsParameter(string name) => _parameters.Contains(name);
    }
}

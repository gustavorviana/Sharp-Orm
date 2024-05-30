using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    /// <summary>
    /// Class responsible for creating instances of objects using reflection.
    /// The class will only create an instance of the object, but the values of its properties and fields will not be initialized (except for record types).
    /// </summary>
    internal class ObjectActivator
    {
        private readonly ParamInfo[] objParams;
        public readonly Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectActivator"/> class.
        /// </summary>
        /// <param name="type">The type of the object to be activated.</param>
        /// <param name="reader">The data reader used to retrieve the object's data.</param>
        /// <exception cref="NotSupportedException">Thrown if the type is abstract or no suitable constructor is found.</exception>
        public ObjectActivator(Type type, DbDataReader reader, TranslationRegistry registry)
        {
            if (type.IsAbstract) throw new NotSupportedException("It's not possible to instantiate an abstract type.");

            this.type = type;

            if (
                type.IsValueType
#if NET5_0_OR_GREATER
                || type.GetMethod("<Clone>$") != null
#endif
                )
                this.objParams = this.GetFields(reader, registry) ?? throw new NotSupportedException("A compatible constructor for the received data could not be found.");
        }

        /// <summary>
        /// Gets the appropriate constructor for the type based on the data reader.
        /// </summary>
        /// <param name="reader">The data reader used to match constructor parameters.</param>
        /// <param name="paramsIndex">Array of parameter indexes.</param>
        /// <returns>The matched constructor, or null if none found.</returns>
        private ParamInfo[] GetFields(DbDataReader reader, TranslationRegistry registry)
        {
            var constructors = this.type.GetConstructors().Where(x => x.GetCustomAttribute<QueryIgnoreAttribute>() == null);

            foreach (var constructor in constructors)
            {
                var ctorParams = constructor.GetParameters();
                var indexes = ctorParams.Select(x => FindParamOnDb(x, reader, registry)).Where(x => x != null).ToArray();
                if (indexes.Length == ctorParams.Length)
                    return indexes;
            }

            return null;
        }

        /// <summary>
        /// Gets the index of a parameter in the data reader.
        /// </summary>
        /// <param name="parameter">The parameter info to locate.</param>
        /// <param name="reader">The data reader used to find the parameter index.</param>
        /// <returns>The index of the parameter in the data reader, or -1 if not found.</returns>
        private static ParamInfo FindParamOnDb(ParameterInfo parameter, DbDataReader reader, TranslationRegistry registry)
        {
            try
            {
                var columnIndex = reader.GetIndexOf(GetName(parameter));
                if (columnIndex == -1 || !(registry.GetFor(parameter.ParameterType) is ISqlTranslation translation))
                    return null;

                return new ParamInfo(columnIndex, parameter.ParameterType, translation);
            }
            catch
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif

                return null;
            }
        }

        /// <summary>
        /// Creates an instance of the object using the data from the data reader.
        /// </summary>
        /// <param name="reader">The data reader containing the object's data.</param>
        /// <returns>The created object instance.</returns>
        public object CreateInstance(DbDataReader reader)
        {
            try
            {
                if (this.objParams == null) return Activator.CreateInstance(this.type);

                return Activator.CreateInstance(this.type, GetValues(reader));
            }
            catch (MissingMethodException ex)
            {
                throw new NotSupportedException("A compatible constructor for the received data could not be found.", ex);
            }
        }

        /// <summary>
        /// Gets the values from the data reader corresponding to the constructor parameters.
        /// </summary>
        /// <param name="reader">The data reader containing the object's data.</param>
        /// <returns>An array of values for the constructor parameters.</returns>
        private object[] GetValues(DbDataReader reader)
        {
            if (this.objParams == null)
#if NET45
                return new object[0];
#else
                return Array.Empty<object>();
#endif

            return this.objParams.Select(x => x.GetValue(reader)).ToArray();
        }

        /// <summary>
        /// Gets the name of a parameter, using the <see cref="CtorColumnAttribute"/> if present.
        /// </summary>
        /// <param name="info">The parameter info.</param>
        /// <returns>The name of the parameter.</returns>
        private static string GetName(ParameterInfo info)
        {
            string name = info.GetCustomAttribute<CtorColumnAttribute>()?.Name;
            return string.IsNullOrEmpty(name) ? info.Name : name;
        }

        private class ParamInfo
        {
            public readonly ISqlTranslation translation;
            private readonly Type expectedType;
            private readonly int index;

            public ParamInfo(int index, Type expected, ISqlTranslation translation)
            {
                this.index = index;
                this.expectedType = expected;
                this.translation = translation;
            }

            public object GetValue(DbDataReader reader)
            {
                return this.translation.FromSqlValue(reader[index], this.expectedType);
            }
        }
    }
}

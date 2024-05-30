using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpOrm.Builder.DataTranslation.Reader
{
    /// <summary>
    /// Class responsible for creating instances of objects using reflection.
    /// The class will only create an instance of the object, but the values of its properties and fields will not be initialized (except for record types).
    /// </summary>
    public class ObjectActivator
    {
        private readonly ConstructorInfo constructor;
        private readonly int[] objIndexes;
        public readonly Type type;

        /// <summary>
        /// Indicates if the type is a record. (.NET 5+ only)
        /// </summary>
        public bool IsRecord { get; }

        /// <summary>
        /// Indicates if the type is a value type.
        /// </summary>
        public bool IsValueType => type.IsValueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectActivator"/> class.
        /// </summary>
        /// <param name="type">The type of the object to be activated.</param>
        /// <param name="reader">The data reader used to retrieve the object's data.</param>
        /// <exception cref="NotSupportedException">Thrown if the type is abstract or no suitable constructor is found.</exception>
        public ObjectActivator(Type type, DbDataReader reader)
        {
            if (type.IsAbstract) throw new NotSupportedException("It's not possible to instantiate an abstract type.");

            this.type = type;
#if NET5_0_OR_GREATER
            this.IsRecord = type.GetMethod("<Clone>$") != null;
#endif

            this.constructor = this.GetConstructor(reader, out this.objIndexes) ?? throw new NotSupportedException("A compatible constructor for the received data could not be found.");
        }

        /// <summary>
        /// Gets the appropriate constructor for the type based on the data reader.
        /// </summary>
        /// <param name="reader">The data reader used to match constructor parameters.</param>
        /// <param name="paramsIndex">Array of parameter indexes.</param>
        /// <returns>The matched constructor, or null if none found.</returns>
        private ConstructorInfo GetConstructor(DbDataReader reader, out int[] paramsIndex)
        {
            paramsIndex = null;
            var constructors = this.type.GetConstructors()
                .Where(x => x.GetCustomAttribute<QueryIgnoreAttribute>() == null);

            if (!this.IsRecord && !this.IsValueType)
                return constructors.FirstOrDefault(x => x.GetParameters().Length == 0);

            foreach (var constructor in constructors)
            {
                var ctorParams = constructor.GetParameters();
                var indexes = ctorParams.Select(x => GetParamIndexOnDb(x, reader)).Where(x => x > -1).ToArray();
                if (indexes.Length != ctorParams.Length)
                    continue;

                paramsIndex = indexes;
                return constructor;
            }

            return null;
        }

        /// <summary>
        /// Gets the index of a parameter in the data reader.
        /// </summary>
        /// <param name="parameter">The parameter info to locate.</param>
        /// <param name="reader">The data reader used to find the parameter index.</param>
        /// <returns>The index of the parameter in the data reader, or -1 if not found.</returns>
        private static int GetParamIndexOnDb(ParameterInfo parameter, DbDataReader reader)
        {
            try
            {
                var columnIndex = reader.GetOrdinal(GetName(parameter));
                return columnIndex < 0 || reader.GetFieldType(columnIndex) != parameter.ParameterType ? -1 : columnIndex;
            }
            catch
            {
#if DEBUG
                System.Diagnostics.Debugger.Break();
#endif

                return -1;
            }
        }

        /// <summary>
        /// Creates an instance of the object using the data from the data reader.
        /// </summary>
        /// <param name="reader">The data reader containing the object's data.</param>
        /// <returns>The created object instance.</returns>
        public object CreateInstance(DbDataReader reader)
        {
            return constructor.Invoke(GetValues(reader));
        }

        /// <summary>
        /// Gets the values from the data reader corresponding to the constructor parameters.
        /// </summary>
        /// <param name="reader">The data reader containing the object's data.</param>
        /// <returns>An array of values for the constructor parameters.</returns>
        private object[] GetValues(DbDataReader reader)
        {
            if (this.objIndexes == null)
#if NET45
                return new object[0];
#else
                return Array.Empty<object>();
#endif

            return this.objIndexes.Select(x => reader[x]).ToArray();
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
    }
}

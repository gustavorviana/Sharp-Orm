using SharpOrm.Msg;
using System;
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
        private readonly ConstructorInfo _ctor;
        private readonly ParamInfo[] _objParams;
        public readonly Type _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectActivator"/> class.
        /// </summary>
        /// <param name="type">The _type of the object to be activated.</param>
        /// <param name="record">The data record used to retrieve the object's data.</param>
        /// <exception cref="NotSupportedException">Thrown if the _type is abstract or no suitable constructor is found.</exception>
        public ObjectActivator(Type type, IDataRecord record, TranslationRegistry registry)
        {
            if (type.IsAbstract) throw new NotSupportedException(Messages.ObjectActivator.AbstractType);
            if (type.IsInterface) throw new NotSupportedException(Messages.ObjectActivator.InterfaceType);
            if (type.IsEnum) throw new NotSupportedException(Messages.ObjectActivator.EnumType);
            if (type.IsArray) throw new NotSupportedException(Messages.ObjectActivator.ArrayType);

            _type = type;
            _objParams = GetParams(record, registry, out _ctor) ?? throw new NotSupportedException(Messages.ObjectActivator.NoSuitableConstructor);

            if (_ctor == null)
                throw new NotSupportedException(Messages.ObjectActivator.NoSuitableConstructor);
        }

        /// <summary>
        /// Gets the appropriate constructor for the _type based on the data record.
        /// </summary>
        /// <param name="record">The data record used to match constructor parameters.</param>
        /// <returns>The matched constructor, or null if none found.</returns>
        private ParamInfo[] GetParams(IDataRecord record, TranslationRegistry registry, out ConstructorInfo constructor)
        {
            var constructors = _type.GetConstructors().Where(x => x.GetCustomAttribute<QueryIgnoreAttribute>() == null);

            foreach (var construct in constructors)
            {
                var ctorParams = construct.GetParameters();
                var indexes = ctorParams.Select(x => FindParamOnDb(x, record, registry)).Where(x => x != null).ToArray();
                if (indexes.Length == ctorParams.Length)
                {
                    constructor = construct;
                    return indexes;
                }
            }

            constructor = null;
            return null;
        }

        /// <summary>
        /// Gets the index of a parameter in the data record.
        /// </summary>
        /// <param name="parameter">The parameter info to locate.</param>
        /// <param name="record">The data record used to find the parameter index.</param>
        /// <returns>The index of the parameter in the data record, or -1 if not found.</returns>
        private static ParamInfo FindParamOnDb(ParameterInfo parameter, IDataRecord record, TranslationRegistry registry)
        {
            try
            {
                var columnIndex = record.GetIndexOf(GetName(parameter));
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
        /// Creates an instance of the object using the data from the data record.
        /// </summary>
        /// <param name="record">The data record containing the object's data.</param>
        /// <returns>The created object instance.</returns>
        public object CreateInstance(IDataRecord record)
        {
            var values = _objParams.Select(x => x.GetValue(record)).ToArray();
            return _ctor.Invoke(values);
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
                expectedType = expected;
                this.translation = translation;
            }

            public object GetValue(IDataRecord record)
            {
                return translation.FromSqlValue(record[index], expectedType);
            }
        }
    }
}

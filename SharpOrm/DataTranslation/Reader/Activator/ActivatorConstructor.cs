using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class ActivatorConstructor
    {
        private readonly List<IParamInfo> _parameters;
        private readonly ConstructorInfo _constructor;

        /// <summary>
        /// Number of parameters that have matching values in the database.
        /// </summary>
        public int Matches { get; }

        /// <summary>
        /// Total number of parameters in the constructor.
        /// </summary>
        public int TotalParameters => _parameters.Count;

        /// <summary>
        /// Number of required (non-optional) parameters that were successfully filled.
        /// </summary>
        public int FilledRequiredParameters => TotalParameters - OptionalParameters;

        /// <summary>
        /// Number of optional parameters in this constructor.
        /// </summary>
        public int OptionalParameters { get; }

        private ActivatorConstructor(ConstructorInfo constructor, List<IParamInfo> parameters, int matches, int optional)
        {
            _constructor = constructor;
            _parameters = parameters;
            Matches = matches;
            OptionalParameters = optional;
        }

        /// <summary>
        /// Tries to create an ActivatorConstructor from the given constructor.
        /// Returns false if any required parameter name does not match (case-insensitive) any available property.
        /// </summary>
        public static bool TryParse(ConstructorInfo constructor, TranslationRegistry registry, IDataRecord record, string prefix, out ActivatorConstructor activatorConstructor)
        {
            activatorConstructor = null;

            var availableProperties = GetAvailableProperties(constructor.DeclaringType);
            var ctorParams = constructor.GetParameters();
            var parameters = new List<IParamInfo>();
            var matches = 0;
            var optionalCount = 0;

            for (int i = 0; i < ctorParams.Length; i++)
            {
                var parameter = ctorParams[i];
                var paramType = parameter.ParameterType;
                var isOptional = parameter.IsOptional;

                if (isOptional)
                    optionalCount++;

                if (!availableProperties.Contains(parameter.Name))
                    return false;

                var paramInfo = GetInfo(parameter, registry, record, prefix, out var dbMatch, out var optional);
                if (paramInfo == null)
                    return false;

                parameters.Add(paramInfo);

                if (dbMatch)
                    matches++;

                if (optional)
                    optionalCount++;
            }

            activatorConstructor = new ActivatorConstructor(constructor, parameters, matches, optionalCount);
            return true;
        }

        private static IParamInfo GetInfo(ParameterInfo parameter, TranslationRegistry registry, IDataRecord record, string prefix, out bool dbMatch, out bool optional)
        {
            dbMatch = false;
            optional = false;
            var fullName = GetName(parameter, prefix);
            if (parameter.ParameterType.GetCustomAttribute<OwnedAttribute>() != null)
            {
                dbMatch = true;
                return new OwnedParamInfo(record, registry, parameter.ParameterType, fullName);
            }

            var columnIndex = record.GetIndexOf(fullName);
            if (columnIndex == -1 || !(registry.GetFor(parameter.ParameterType) is ISqlTranslation translation))
                return null;

            dbMatch = true;
            return new ParamInfo(parameter, record, columnIndex, translation);
        }

        private static HashSet<string> GetAvailableProperties(Type type)
        {
            return new HashSet<string>(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the name of a parameter, using the <see cref="CtorColumnAttribute"/> if present.
        /// </summary>
        /// <param name="info">The parameter info.</param>
        /// <returns>The name of the parameter (without prefix).</returns>
        private static string GetName(ParameterInfo info, string prefix)
        {
            return string.IsNullOrEmpty(prefix) ? info.Name : $"{prefix}_{info.Name}";
        }

        public object Invoke()
        {
            var values = new object[_parameters.Count];
            for (int i = 0; i < _parameters.Count; i++)
                values[i] = _parameters[i].GetValue();

            return _constructor.Invoke(values);
        }

        public bool IsSame(ActivatorConstructor constructor)
        {
            return Matches == constructor.Matches &&
                FilledRequiredParameters == constructor.FilledRequiredParameters &&
                TotalParameters == constructor.TotalParameters;
        }
    }
}

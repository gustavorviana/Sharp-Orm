using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SharpOrm.DataTranslation.Reader.Activator
{
    internal class ActivatorConstructor
    {
        private readonly IReadOnlyList<IParamInfo> _parameters;
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

        private ActivatorConstructor(ConstructorInfo constructor, IReadOnlyList<IParamInfo> parameters, int matches, int optional)
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
        public static bool TryParse(ParameterBinder binder, ConstructorInfo constructor, out ActivatorConstructor activatorConstructor)
        {
            activatorConstructor = null;
            if (!binder.FindAll(constructor))
                return false;

            activatorConstructor = new ActivatorConstructor(constructor, binder.GetParameters(), binder.Matches, binder.OptionalCount);
            return true;
        }

        public object Invoke()
        {
            var values = new object[_parameters.Count];
            for (int i = 0; i < _parameters.Count; i++)
                values[i] = _parameters[i].GetValue();

            return _constructor.Invoke(values);
        }

        public string[] GetParameterNames()
        {
            var names = new string[_parameters.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _parameters[i].Name;

            return names;
        }

        public bool IsSame(ActivatorConstructor constructor)
        {
            return Matches == constructor.Matches &&
                FilledRequiredParameters == constructor.FilledRequiredParameters &&
                TotalParameters == constructor.TotalParameters;
        }
    }
}
